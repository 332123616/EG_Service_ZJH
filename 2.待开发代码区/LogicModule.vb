Imports System.Collections.Concurrent
Imports System.Threading
Imports Dll_ToolModule

''' <summary>
''' 逻辑代码模块
'''发送数据方法 userclass.senddata(数据)
''' </summary>
Module LogicModule
#Region "变量"
    ''' <summary>
    ''' 游戏局数
    ''' </summary>
    Private GameNum As Integer = 0
    ''' <summary>
    ''' 游戏关数
    ''' </summary>
    Private GameGuan As Integer = 0
    ''' <summary>
    ''' 版本号
    ''' </summary>
    Private Version As String = "2017.10.11.0"
    ''' <summary>
    ''' 开放的机台数
    ''' </summary>
    Private GameMachineCount As Integer = 80
    Public Arith(4) As ArithmeticClass

#End Region
    ''' <summary>
    ''' 初始化
    ''' </summary>
    Public Sub ItemInitialise()
        SelectDB.Start()
        GameStatus.Start()
        '1区
        Arith(1) = New ArithmeticClass()
        '2区
        Arith(2) = New ArithmeticClass()
        '3区
        Arith(3) = New ArithmeticClass()
        '4区
        Arith(4) = New ArithmeticClass()
        Dim haha As New UserClass()
        haha.Bet = "0/10/0/10/0/0/0"
        'Arith(1).UserDic.TryAdd("hh", haha)
    End Sub

    ''' <summary>
    '''  用户连入事件（新用户）
    ''' </summary>
    Public Sub UesrNewConnEvent(ByRef _User As UserClass)
        'RZ?校验码@账号@积分
        Arith(_User.Zone).UserDic.TryAdd(_User.CheckCode, _User)
    End Sub

    ''' <summary>
    ''' 用户返回到大厅
    ''' </summary>
    ''' <param name="_User"></param>
    Public Sub UserDisconnEvent(ByRef _User As UserClass)
        Arith(_User.Zone).UserDic.TryRemove(_User.CheckCode, Nothing)
    End Sub

    ''' <summary>
    ''' 用户接收数据事件
    ''' </summary>
    ''' <param name="_Data">数据内容</param>
    ''' <param name="_User">用户</param>
    Public Sub UesrReceiveEvent(ByVal _Data As String, ByRef _User As UserClass)
        Try
            Dim _Tem As String() = _Data.Split("?")
            Select Case _Tem(0)
                Case "OK"
                    Dim _Init As String = Arith(_User.Zone).Init(_User)
                    _User.SendData("INIT?" & _Init)
                Case "XZ" 'XZ?0/0/0/0/0/0/0
                    '不是下注倒计时阶段不让下注 
                    If Arith(_User.Zone).IsBet = False Then Exit Sub
                    Arith(_User.Zone).UpdateTotalBet(_User, _Tem(1))
                    ' Arith(_User.Zone).UserDic(_User.CheckCode).Bet = _Tem(1)
            End Select
        Catch ex As Exception
            DBErrWrite("LogicModule->UesrReceiveEvent 玩家名称:" & _User.Account, ex)
        End Try
    End Sub
    ''' <summary>
    ''' 结算，中心服务器返回信息触发此过程
    ''' </summary>
    ''' <param name="_Data"></param>
    Public Sub SettleAccountsInfo(ByVal _Data As String)
        Try
            Dim _Arr() As String = _Data.Split("@")
            'JS?用户帐号@真实额度
            Dim _User As UserClass = GetUser(_Arr(0))
            If IsNothing(_User) Then Exit Sub
            If Val(_Arr(1)) < 0 Then _User.Integral = 0 Else _User.Integral = Format(Val(_Arr(1)), "0.00")
            '发送输赢分数 JS？中奖分数，最后的剩余额度
            _User.SendData("JS?" & _User.Score & "," & _User.Integral)
            Console.WriteLine("JS?" & _User.Score & "," & _User.Integral)
        Catch ex As Exception
            DBErrWrite("LogicModule->SettleAccountsInfo 结算抛出异常", ex)
        End Try
    End Sub
    ''' <summary>
    ''' 通过账号寻找玩家
    ''' </summary>
    ''' <param name="_Account">账号</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetUser(ByVal _Account As String) As UserClass
        For Each Item In UserList.Values
            If Item.Account = _Account Then
                Return Item
            End If
        Next
        Return Nothing
    End Function
    ''' <summary>
    ''' 向中心服务器发送数据
    ''' </summary>
    Public Sub SendDataToCenter(ByVal _Data As String)
        ManageServer.LoginServiceSocket.SendAppData(_Data)
    End Sub
    ''' <summary>
    ''' 查询配置表的线程
    ''' </summary>
    Public SelectDB As New Thread(AddressOf SelectConfig)
    Private Sub SelectConfig()
        While True
            Thread.Sleep(3000)

        End While
    End Sub
    ''' <summary>
    ''' 游戏状态线程 1秒1循环 给登入服务器发送各个分区的状态数据
    ''' </summary>
    Public GameStatus As New Thread(AddressOf fGameStatus)
    Private Sub fGameStatus()
        Dim _ZT As String = ""
        While True
            Thread.Sleep(1000)
            '如果任何一个大区有人则发送状态数据
            If (Arith(1).UserDic.Count > 0 OrElse Arith(2).UserDic.Count > 0 OrElse Arith(3).UserDic.Count > 0 OrElse Arith(4).UserDic.Count > 0) AndAlso Maintenance = False Then
                Dim _One As String = Arith(1).Status & "/" & Arith(1).MinBet & "/" & Arith(1).MaxBet & "/" & Arith(1).CardRoadNote.Trim("#")
                Dim _Two As String = Arith(2).Status & "/" & Arith(2).MinBet & "/" & Arith(2).MaxBet & "/" & Arith(2).CardRoadNote.Trim("#")
                Dim _Three As String = Arith(3).Status & "/" & Arith(3).MinBet & "/" & Arith(3).MaxBet & "/" & Arith(3).CardRoadNote.Trim("#")
                Dim _Four As String = Arith(4).Status & "/" & Arith(4).MinBet & "/" & Arith(4).MaxBet & "/" & Arith(4).CardRoadNote.Trim("#")
                Dim _Status As String = _One & "," & _Two & "," & _Three & "," & _Four
                '如果新的状态和旧的不一样则发送状态
                If _Status <> _ZT Then
                    _ZT = _Status
                    SendDataToCenter("CD?" & EGTag & "," & _Status)
                    Console.WriteLine("CD?" & EGTag & "," & _Status)
                End If

            End If
        End While
    End Sub


End Module
