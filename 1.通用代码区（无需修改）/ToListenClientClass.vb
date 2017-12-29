Imports Dll_ToolModule
Imports System.Collections.Concurrent
Imports System.Threading
''' <summary>
''' 程序名称：处理转接器发送数据模块
''' 程序作用：
''' 1、与转接器
''' 更新记录：
''' 2017/08/30 修改人：孙哥 修改内容：重构开发。
''' </summary>
Public Class ToListenClientClass
    ''' <summary>
    ''' 监听APP连入的服务器SOCKET
    ''' </summary>
    Private WithEvents ListenServer As ServerSocketClass

    ''' <summary>
    ''' 构造方法
    ''' </summary>
    Public Sub New(ByVal _ListenIP As String, ByVal _ListenPort As Integer, ByVal _ListenCount As Integer)
        ListenServer = New ServerSocketClass(_ListenIP, _ListenPort, _ListenCount)
        ListenServer.StartListen()
        '未操作控制线程
        NoActiveTh.Start()
        '心跳验证线程
        MsgThread.Start()
    End Sub
    ''' <summary>
    ''' 销毁本类
    ''' </summary>
    Public Sub Dispose()
        ListenServer.Dispose()
        UserList.Clear()
    End Sub

    ''' <summary>
    ''' 当有接收应用层数据自动触发
    ''' </summary>
    ''' <param name="_Msgstr"></param>
    ''' <param name="_SocketUser"></param>
    Private Sub UesrReceiveAppEvent(ByVal _Msgstr As String, ByVal _SocketUser As ServerUserClass) Handles ListenServer.UesrReceiveAppEvent
        '数据格式 (校验码 周+小时+分+秒+毫秒)
        '用途心跳 CH>游戏服务器标记>校验码>信息序号>转接器标记
        '转发数据 ZF>游戏服务器标记>校验码>信息序号>转接器标记>信息标头?内容
        Try
            '拆分接收数据
            Dim _Data() As String = _Msgstr.Split(">")
            Dim _CheckCode As String = _Data(2)
            Dim _MsgNum As String = _Data(3)
            Dim _CommutatorMark As String = _Data(4)
            '如果登入服务没有向本游戏发送，玩家登入信息，那么 本校验失败，此消息作废
            If UserList.ContainsKey(_CheckCode) = False Then Exit Sub '-----Exit Sub

            Dim _User As UserClass = UserList(_CheckCode)

            '判断用户是否保存该转接器  未保存则将转接器标记和socket加入字典   保存则重置该条转接器socket
            If _User.SocketDic.ContainsKey(_CommutatorMark) = False Then
                _User.SocketDic.TryAdd(_CommutatorMark, _SocketUser)
            Else
                _User.SocketDic(_CommutatorMark) = _SocketUser '重置该条转接器socket
            End If
            '判断用户之前是否掉线 如果是 判断为重连用户 触发重连事件
            If _User.OnLine = False Then
                _User.OnLine = True  'UesrReconnEvent(_User) '触发重连事件
            End If

            _User.HeartTimes = 0
            '根据数据格式，做相应处理        
            Select Case _Data(0)
                Case "CH" '心跳无需验证
                    _SocketUser.SendAppData(_Msgstr) '返回心跳数据
                Case "ZF"
                    '#为确保往返数据，均正确，无丢失，在这里我们使用了消息序列，并加入验证逻辑，如果有发送失败的将持续发送，直到出现成功

                    '处理 服务器(发送数据)-->客户端(接收处理，发送回执)-->服务器(处理回执) 的回执信息
                    Dim _Tmp As String() = _Data(5).Split("?")
                    If _Tmp(0) = "HZ" Then RemoveMsgList(_CheckCode & _Tmp(1)) : Exit Sub
                    '接收到消息  回执信息 仅发送1个数据(3个转接器收到3次，也发3次)
                    _SocketUser.SendAppData("ZF>" & EGTag & ">" & _CheckCode & ">0>" & "HZ?" & _MsgNum)

                    '消息过滤 相同序号的消息处理先到者 将(校验码，消息序号集合)存入一个字典
                    '判断该消息序号是否包含在集合中 包含说明该消息已被处理 直接跳出
                    SyncLock _User.FilterLock
                        If _User.MsgNumArray.Contains(_MsgNum) = False Then
                            '如果是最先到消息，将消息序号加入集合
                            _User.MsgNumArray.Add(_MsgNum)
                        Else
                            Exit Sub
                        End If
                    End SyncLock

                    '如果信息集合数量大于200 删除前50条
                    If _User.MsgNumArray.Count > 200 Then _User.MsgNumArray.RemoveRange(0, 50)
                    _User.ActiveTime = Now '记录活跃时间
                    UesrReceiveEvent(_Data(5), _User)
            End Select
        Catch ex As Exception
            DBErrWrite("ToListenClientClass->UesrReceiveAppEvent方法->接收应用层数据发生错误,接收数据：" & _Msgstr, ex)
        End Try
    End Sub
    ''' <summary>
    ''' 接收消息锁
    ''' </summary>
    Public MsgListLock As New Object
    ''' <summary>
    ''' 与转接器连接成功时触发
    ''' </summary>
    Private Sub ListenServerConnectedEvent(ByVal _user As ServerUserClass) Handles ListenServer.UesrConnectedEvent
        DBSubWrite("与转接器IP" & _user.UserIP & "--已连入")
        Console.WriteLine("与转接器IP" & _user.UserIP & "--已连入")
    End Sub
    ''' <summary>
    ''' 与转接器断线时触发
    ''' </summary>
    Private Sub ListenServerLeaveEvent(ByVal _user As ServerUserClass) Handles ListenServer.UesrLeaveEvent
        DBSubWrite("与转接器IP" & _user.UserIP & "--已断开")
    End Sub


    ''' <summary>
    ''' 长时间未操作控制线程
    ''' </summary>
    Private NoActiveTh As Thread = New Thread(AddressOf NoActiveTh1)
    ''' <summary>
    ''' 长时间未操作方法 59分钟踢掉
    ''' </summary>
    Private Sub NoActiveTh1()
        '限定时长
        Dim outtime As Integer = 300 '3540
        While True
            Try
                '遍历用户字典
                For Each user As UserClass In UserList.Values
                    '用户处于连接状态
                    If user.OnLine = True Then
                        '获取时间差
                        Dim spTime As TimeSpan = Now - user.ActiveTime
                        '时间超时
                        If spTime.TotalSeconds > outtime Then
                            '向登陆服务器发送提出请求
                            ManageServer.LoginServiceSocket.SendAppData("TC?" & user.CheckCode)
                            '向客户端发送返回大厅指令
                            user.SendData("OUT?0")
                            '用户断线逻辑
                            UserDisconnEvent(user)
                            '销毁socket
                            user.Dispose()
                            '将该用户删除
                            UserList.TryRemove(user.CheckCode, Nothing)
                        End If
                    End If
                Next
            Catch ex As Exception
                DBErrWrite("ToListenClientClass->NoActiveTh1方法", ex)
            End Try
            '10秒一次
            Threading.Thread.Sleep(10000)
        End While
    End Sub
    ''' <summary>
    ''' 消息序列线程
    ''' </summary>
    Private MsgThread As Thread = New Thread(AddressOf MsgThreadSub)
    ''' <summary>
    ''' 消息序列线程
    ''' </summary>
    Private Sub MsgThreadSub()
        '是否溢出，当消息序列中数据量大于500条，那么记录为溢出
        Dim _IfOverflow As Boolean = False
        Do While True
            Try
                If MsgList.Count > 0 Then
                    For Each _Value As MsgClass In MsgList.Values
                        '在40秒内 没有回执 那么持续发送（本线程间隔时间是50毫秒 40秒*每秒循环20次=800）
                        '因为前端30秒没有连线 就彻底掉线了，所以我们在40秒进行删除
                        If _Value.IfReturn = False AndAlso _Value.Num <= 800 Then
                            _Value.Num += 1 '计数+1
                            '计数每5次 发送一次
                            If _Value.Num Mod 5 = 1 Then _Value.User.SendData1(_Value.MsgStr)
                        Else
                            '有回执那么删除本消息
                            MsgList.TryRemove(_Value.Key, Nothing)
                            _Value.Dispose()
                        End If
                    Next
                    If _IfOverflow = False AndAlso MsgList.Count > 500 Then
                        _IfOverflow = True
                        DBErrWrite("消息序列数据过多：" & MsgList.Count, Nothing)
                    End If
                End If
            Catch ex As Exception '本方法中应该不会有异常，如果出现进行处理，然后在删除下面代码
                DBErrWrite("MsgThreadSub出现异常", ex)
            End Try
            Threading.Thread.Sleep(50)
        Loop
    End Sub
    ''' <summary>
    ''' 当接收到回执后，删除消息序列中数据
    ''' </summary>
    ''' <param name="_Key"></param>
    Private Sub RemoveMsgList(ByVal _Key As String)
        '网络延迟情况下，本方法,有可能同时被调用多次，很小时间差时会出现找不到对象
        Try
            If MsgList.ContainsKey(_Key) = True Then MsgList(_Key).IfReturn = True
        Catch ex As Exception
        End Try
    End Sub
End Class
''' <summary>
''' 消息类
''' </summary>
Public Class MsgClass
    Public User As UserClass
    Public Key As String
    Public MsgStr As String '消息
    Public IfReturn As Boolean '是否有回执
    Public Num As Integer '计数（利用计数进行发送数据）

    Public Sub New(ByRef _User As UserClass, ByRef _Key As String, ByRef _MsgStr As String)
        User = _User : Key = _Key : MsgStr = _MsgStr
    End Sub

    Public Sub Dispose()
        User = Nothing
        Key = Nothing
        MsgStr = Nothing
    End Sub
End Class
