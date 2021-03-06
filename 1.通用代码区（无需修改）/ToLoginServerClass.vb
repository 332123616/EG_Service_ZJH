﻿Imports Dll_ToolModule
''' <summary>
''' 程序名称：与登入服务交互类
''' 程序作用：
''' 1、与登入服务 进行数据交互的类
''' 更新记录：
''' 2016/11/15 修改人：阿森 修改内容：开发
''' 2017/08/30 修改人：孙哥 修改内容：重构开发。
''' </summary>
Public Class ToLoginServerClass
    ''' <summary>
    ''' 客户端SOCKET对象，用于连接登入服务器
    ''' </summary>
    Public WithEvents LoginServiceSocket As ClientSocketClass

    ''' <summary>
    ''' 构造方法，同时开启连接登入服务器
    ''' </summary>
    ''' <param name="_ServerIP"></param>
    ''' <param name="_ServerPort"></param>
    Public Sub New(ByVal _ServerIP As String, ByVal _ServerPort As Integer)
        LoginServiceSocket = New ClientSocketClass(_ServerIP, _ServerPort)
        LoginServiceSocket.StartConnect()
    End Sub
    ''' <summary>
    ''' 销毁本类
    ''' </summary>
    Public Sub Dispose()
        LoginServiceSocket.Dispose()
    End Sub
    ''' <summary>
    ''' 连接登入服务器成功时触发
    ''' </summary>
    Private Sub ConnectedEvent() Handles LoginServiceSocket.ConnectedEvent
        '每次与登入服务器连接成功后，首先发送 游戏标记
        LoginServiceSocket.SendAppData("LR?" & EGTag)
        DBSubWrite(EGTag & "服务 与 登入服务 连接成功")
        Console.WriteLine(EGTag & "服务 与 登入服务 连接成功")
    End Sub
    ''' <summary>
    ''' 掉线触发
    ''' </summary>
    Private Sub LeaveEvent() Handles LoginServiceSocket.LeaveEvent
        DBSubWrite(EGTag & "服务 与 登入服务 断开连接")
    End Sub

    ''' <summary>
    ''' 接收数据触发
    ''' </summary>
    ''' <param name="_Msgstr"></param>
    Private Sub ReceiveEvent(ByVal _Msgstr As String) Handles LoginServiceSocket.ReceiveAppEvent
        Try
            Dim _Data() As String = _Msgstr.Split("?")
            Select Case _Data(0)
                Case "RZ" '@ 玩家要进入本游戏，登入服务器先通知 游戏服务，当游戏服务返回信息后玩家才可进入 #数据格式 RZ?校验码@座位号@账号@积分@经销商序号@分站序号@代理账号@昵称@玩家等级(用于退水)@是否测试账号@设备字符串
                    '# 数据声明
                    Dim _Tmp() As String = _Data(1).Split("@")
                    Dim _CheckCode As String = _Tmp(0) '校验码
                    Dim _SeatNum As Integer = Val(_Tmp(1)) '座位号                 
                    Dim _UserName As String = _Tmp(2) '账号
                    Dim _Integral As Double = Val(_Tmp(3)) '积分
                    Dim _Dealer As Integer = Val(_Tmp(4)) '经销商序号
                    Dim _Substation As Integer = Val(_Tmp(5)) '分站序号
                    Dim _Agent As String = _Tmp(6) '代理账号
                    Dim _Nick As String = _Tmp(7) '昵称
                    Dim _UserRank As Integer = Val(_Tmp(8)) '玩家等级(用于退水)
                    Dim _AccountType As Integer = Val(_Tmp(9)) '是否测试账号
                    Dim _Device As String = _Tmp(10) '设备字符串
                    Dim _User As UserClass = Nothing

                    '# 判断是否有重连逻辑，如果有找到对应用户对象
                    'Dim _IfReconn As Boolean = False '是否有重连逻辑 默认无断线重连逻辑

                    If _Tmp(11) = "1" Then
                        '帐号 相同的 断线重连逻辑
                        For Each _OtherUser As UserClass In UserList.Values
                            If _OtherUser.Account = _UserName AndAlso _OtherUser.Dealer = _Dealer AndAlso _OtherUser.Substation = _Substation Then
                                If _OtherUser.IsPlay = True Then
                                    '进行更换验证码
                                    UserList.TryRemove(_OtherUser.CheckCode, Nothing)
                                    _User = _OtherUser
                                    _User.OnLine = True
                                    _User.MsgNumArray = New ArrayList()
                                    _User.CheckCode = _CheckCode
                                    UserList.TryAdd(_CheckCode, _User)
                                    '  UesrReconnEvent(_User) '触发重连事件
                                    ' _IfReconn = True '进行重连逻辑
                                Else
                                    UserList.TryRemove(_OtherUser.CheckCode, Nothing)
                                    _OtherUser.Dispose()
                                End If
                                Exit For
                            End If
                        Next
                        'Else
                        '    '校验码 相同的 断线重连逻辑
                        '    _User = UserList(_CheckCode)
                        '    _User.MsgNumArray = New ArrayList()
                        '    For Each item In _User.SocketDic.Values
                        '        If item IsNot Nothing Then item = Nothing
                        '    Next
                        '    _User.OnLine = True
                        '    UesrReconnEvent(_User) '触发重连事件
                        '    _IfReconn = True '进行重连逻辑
                    End If
                    '如果没有重连逻辑，那么新建帐号
                    If _Tmp(11) = "0" Then
                        _User = New UserClass()
                        UserList.TryAdd(_CheckCode, _User)
                        _User.OnLine = True
                    End If
                    '# 重新赋值
                    _User.CheckCode = _CheckCode
                    _User.Account = _UserName
                    _User.Integral = _Integral
                    _User.Dealer = _Dealer
                    _User.Substation = _Substation
                    _User.Agent = _Agent
                    _User.Nick = _Nick
                    _User.UserRank = _UserRank
                    _User.AccountType = _AccountType
                    _User.Device = _Device
                    '不是重连逻辑，才会触发新用户连接事件
                    If _Tmp(11) = "0" Then
                        _User.MachineNumber = _SeatNum
                        UesrNewConnEvent(_User)
                        '返回登录服务器 RZ?用户校验码@座位号@帐号@积分@游戏标记
                        LoginServiceSocket.SendAppData("RZ?" & _CheckCode & "@" & _SeatNum & "@" & _UserName & "@" & _Integral & "@" & EGTag)
                    End If

                Case "JS" '#中心服务器返回结算信息
                    LogicModule.SettleAccountsInfo(_Data(1))
                Case "TC" '玩家离开本游戏，登入服务器通知游戏服务器 数据格式 TC>校验码
                    Dim _CheckCode As String() = _Data(1).Split(",")
                    If UserList.ContainsKey(_CheckCode(0)) = True Then
                        Dim _User As UserClass = UserList(_CheckCode(0))
                        UserDisconnEvent(_User)
                        If _CheckCode(1) = "1" Then LoginServiceSocket.SendAppData("QT?" & _CheckCode(0))
                        UserList.TryRemove(_CheckCode(0), Nothing) '在集合中删除
                        _User.Dispose() '销毁内部对象                 
                    End If
                Case "WH"
                    Maintenance = _Data(1)
            End Select
        Catch ex As Exception
            DBErrWrite("ReceiveEvent", ex)
        End Try
    End Sub

End Class
