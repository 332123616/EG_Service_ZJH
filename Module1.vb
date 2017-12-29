Imports NUnit.Framework
Imports System.Threading
Imports System.Collections.Concurrent
Imports Dll_ToolModule
Module Module1

    ''' <summary>
    ''' TestFixture:表明这是一个需要测试的类
    ''' Category：自己起的名，方便类的区分和测试
    '''
    ''' </summary>
    <TestFixture>
    <Category("测试ZYJZ算法类")>
    Public Class ZYJZArithmeticTest
        Public test As TestArithmeticClass = New TestArithmeticClass()
        ''' <summary>
        ''' SetUp:初始化测试环境，本例中为读取Config.XML文件信息，以及数据库的读取和存储操作
        ''' 定义：测试函数的初始化函数，每个测试函数运行前都会被调用一次
        ''' NUnit运行时第一步走这里，
        ''' 测试用例编写步骤一
        ''' </summary>
        <SetUp>
        Public Sub MainStartGame()
            Dim haha As New UserClass()
            haha.Account = "poqiang"
            haha.Bet = "0/10/0/10/0/0/0"
            test.UserDic.TryAdd("haha", haha)
        End Sub

        ''' <summary>
        ''' 测试算法：ArithmeticClass-->GameStart
        ''' </summary>
        <Category("GameStart")>
        <Test>
        Public Sub TestArithmetic()
            '执行
            For i As Integer = 0 To 100000
                test.GameStart()
            Next
            'Assert.IsTrue(0)
            ' Debug.Print(0)
        End Sub
    End Class


    ''' <summary>
    ''' 游戏算法类
    ''' </summary>
    Public Class TestArithmeticClass
        ''' <summary>
        ''' 用户字典
        ''' </summary>
        Public UserDic As New ConcurrentDictionary(Of String, UserClass)
        ''' <summary>
        ''' 游戏局数
        ''' </summary>
        Public GameNum As Integer = 1
        ''' <summary>
        ''' 2副牌的数组 每幅三张牌
        ''' </summary>
        Public Card(1) As String
        ''' <summary>
        ''' 游戏状态
        ''' </summary>
        Public Status As String = "未开局"
        ''' <summary>
        ''' 是否可以下注
        ''' </summary>
        Public IsBet As Boolean = False
        ''' <summary>
        ''' 最小下注
        ''' </summary>
        Public MinBet As Double = 500
        ''' <summary>
        ''' 最大下注
        ''' </summary>
        Public MaxBet As Double = 50000
        ''' <summary>
        ''' 人数上限
        ''' </summary>
        Public MaxUser As Integer = 100
        ''' <summary>
        ''' 座位号集合
        ''' </summary>
        Public Seat As New List(Of Integer)
        ''' <summary>
        ''' 牌路记录
        ''' </summary>
        Public CardRoadNote As String = ""
        ''' <summary>
        ''' 牌型记录
        ''' </summary>
        Public CardTypeNote As String = ""

        ''' <summary>
        ''' 初始化方法 当玩家进入该分区时调用
        ''' </summary>
        Public Function Init(ByRef _User As UserClass) As String
            Dim _Res As String = ""
            Try
                '座位号
                Dim _Seat As Integer = 0
                For i As Integer = 0 To Seat.Count - 1
                    If Seat(i) = 0 Then
                        _Seat = i + 1
                        Exit For
                    End If
                Next
                _User.Seat = _Seat
                '座位号，游戏局，账号，积分，下注最小值，下注最大值,牌路记录(/拆分)
                _Res = _Seat & "," & GameNum & "," & _User.Account & "," & _User.Integral & "," & MinBet & "," & MaxBet & "," & CardRoadNote.Trim("#") & "," & CardTypeNote.Trim("#")
            Catch ex As Exception
                Console.WriteLine(ex.Message)
            End Try
            Return _Res
        End Function
        Sub New()
            'GameThread.Start()
            NpcThread.Start()
        End Sub

        Private NpcThread As New Thread(AddressOf NpcPlay)
        Private Sub NpcPlay()
            While True
                If Seat.Count < MaxUser Then
                    Dim _Add As Integer = MaxUser - Seat.Count
                    For i As Integer = 1 To _Add
                        Seat.Add(0)
                    Next
                End If
                Thread.Sleep(3000)
                Dim _RoomSeat As String = "" '= ManageDB.SelectRoomSeat()
                If _RoomSeat <> "" Then
                    '循环座位信息初始化npc用户添加到字典
                    Dim _SeatArr As String() = _RoomSeat.Split(",")
                    For i As Integer = 0 To _SeatArr.Length - 1
                        Dim _Seat As Integer = i + 1
                        If _SeatArr(i) = 2 AndAlso UserDic.ContainsKey(_Seat) = False Then
                            '查询Npc账号
                            Dim _Table As DataTable = ManageDB.SelectNpcInfo()
                            Dim _Row As DataRow = _Table.Rows(0)
                            Dim _user = New UserClass()
                            _user.CheckCode = "NPC"
                            ' _user.MachineNumber = _Seat
                            _user.Nick = CStr(_Row("_nNick"))
                            _user.Dealer = CStr(_Row("_iDealer"))
                            _user.Substation = CStr(_Row("_iSubregion"))
                            _user.Account = CStr(_Row("_vUserName"))
                            _user.Integral = 10000000
                            _user.OnLine = True
                            _user.UserRank = "1"
                            'UserDic.TryAdd(_Seat, _user)
                        ElseIf _SeatArr(i) <> 2 AndAlso UserDic.ContainsKey(_Seat) = True Then
                            '移除npc座位号
                            'UserDic.TryRemove(_Seat, Nothing)
                        End If
                    Next
                End If

            End While
        End Sub
        ''' <summary>
        ''' 游戏局数线程，一关一循环
        ''' </summary>
        Public GameThread As New Thread(AddressOf GameStart)
        Public Sub GameStart()
            ' While True
            If UserDic.Count > 0 Then
                '一局一洗牌             
                WashCard()
                '启动倒计时
                SendDataToAll("BET?")
                'Thread.Sleep(3000)
                '计数
                IsBet = True
                Status = "下注中"
                For i As Integer = 20 To 0 Step -1
                    SendDataToAll("TIME?" & i)
                    '倒数5秒中
                    If i <= 5 Then Status = "倒数" & i & "秒"
                    'Thread.Sleep(1000)
                Next
                IsBet = False
                '倒计时结束
                SendDataToAll("OVER?")
                'Thread.Sleep(3000)
                '延迟3秒后发牌
                Status = "发牌中"
                SendDataToAll("CARD?" & Card(0) & "," & Card(1))
                '牌发完了 20秒后开始算分，分别向各个玩家发送输赢结算数据
                ' Thread.Sleep(20000)
                Status = "结算中"
                '算分
                JudgeScore(Card)
                ' Thread.Sleep(5000)
                SendDataToAll("JL?" & CardRoadNote.Trim("#") & "," & CardTypeNote.Trim("#"))
                GameNum += 1
            Else
                Thread.Sleep(3000)
            End If
            ' End While
        End Sub
        ''' <summary>
        ''' 算分
        ''' </summary>
        Public Sub JudgeScore(ByVal _Card As String())
            '龙凤输赢信息 0:A|3:0|0
            Dim _Tem As String = FailWin(_Card)
            Dim _Info As String() = _Tem.Split("|")
            '添加路单逻辑 ################上限后期再定，此处暂时不添加该逻辑######################
            CardRoadNote &= "#" & _Info(2)
            '添加牌型记录
            CardTypeNote &= "#" & _Tem
            '谁赢使用谁的牌
            Dim _WhoCard As String() = {}
            If _Info(2) = "0" Then
                _WhoCard = _Info(0).Split(":")
            ElseIf _Info(2) = "1" Then
                _WhoCard = _Info(1).Split(":")
            End If
            '便利玩家分别发送结算信息
            For Each _User In UserDic.Values
                '玩家下注信息 下注格式： 0/0/0/0/0/0/0
                If _User.Bet = "" Then Continue For
                Dim _Bet As String() = _User.Bet.Split("/")
                '总下注多少分
                Dim _TotalBet As Double = 0
                For Each _Item In _Bet
                    If Val(_Item) > 0 Then _TotalBet += Val(_Item)
                Next
                '如果下注了则开始算分 否则跳过下面代码继续下一个玩家
                If _TotalBet = 0 Then Continue For
                '该玩家的总得分
                Dim _Win As Double = 0
                '输了多少
                Dim _Lose As Double = 0
                '不是和时计算分数
                If _Info(2) <> 2 Then
                    '算龙的得分情况 1:0.97
                    If Val(_Bet(0)) > 0 Then
                        '龙赢时
                        If _Info(2) = "0" Then
                            '将赢分加入赢中
                            _Win += Val(_Bet(0)) * 0.97
                        ElseIf _Info(2) = "1" Then '凤赢时
                            '将输分加入输中
                            _Lose += Val(_Bet(0))
                        End If
                    End If
                    '算凤的得分情况 1:0.97
                    If Val(_Bet(1)) > 0 Then
                        '凤赢时
                        If _Info(2) = "1" Then
                            '将赢分加入赢中
                            _Win += Val(_Bet(1)) * 0.97
                        ElseIf _Info(2) = "0" Then
                            '龙赢时将输分加入输中
                            _Lose += Val(_Bet(1))
                        End If
                    End If
                    '算8对以上的得分情况 1:2
                    If Val(_Bet(2)) > 0 Then
                        '当类型为对子时需要判断是否是8对以上（不包括8对）
                        If _WhoCard(0) = 1 AndAlso _WhoCard(1) > 8 Then
                            _Win += Val(_Bet(2)) * 2
                        ElseIf _WhoCard(0) > 1 Then '判断类型是否比对子大
                            _Win += Val(_Bet(2)) * 2
                        Else '否则为输分
                            _Lose += Val(_Bet(2))
                        End If
                    End If
                    '算顺子的得分情况
                    If Val(_Bet(3)) > 0 Then
                        If _WhoCard(0) = 2 Then '判断类型是否是顺子
                            _Win += Val(_Bet(3)) * 7
                        Else '否则为输分
                            _Lose += Val(_Bet(3))
                        End If
                    End If
                    '算同花的得分情况
                    If Val(_Bet(4)) > 0 Then
                        If _WhoCard(0) = 3 Then
                            _Win += Val(_Bet(4)) * 8
                        Else '否则为输分
                            _Lose += Val(_Bet(4))
                        End If
                    End If
                    '算同花顺的得分情况
                    If Val(_Bet(5)) > 0 Then
                        If _WhoCard(0) = 4 Then
                            _Win += Val(_Bet(5)) * 100
                        Else '否则为输分
                            _Lose += Val(_Bet(5))
                        End If
                    End If
                    '算豹子的得分情况
                    If Val(_Bet(6)) > 0 Then
                        If _WhoCard(0) = 4 Then
                            _Win += Val(_Bet(6)) * 120
                        Else '否则为输分
                            _Lose += Val(_Bet(6))
                        End If
                    End If
                End If
                _User.Score = Math.Round(_Win - _Lose, 2)
                Console.WriteLine("输赢情况：" & _User.Score)
                'JS?1@游戏标记@帐号@经销商序号@分站序号@代理@总投注@房间比例@退水等级@设备类型@是否测试账号@中奖得分@注单内容@注单类型@实际加分扣分
                Dim _Js As String = "JS?1@" & EGTag & "@" & _User.Account & "@" & _User.Dealer & "@" & _User.Substation & "@" & _User.Agent & "@" & _User.Bet & "@1@" & _User.BackWater & "@" & _User.Device & "@" & _User.AccountType & "@" & _User.Score & "@" & _User.NoteContent & "@" & _User.NoteType & "@" & _User.Score
                'SendDataToCenter(_Js)
            Next
        End Sub

        ''' <summary>
        '''龙与凤比较输赢 返回的数据格  0:A|3:0|0
        '''龙的牌型:点数|凤的牌型:点数|0龙赢了 1凤赢了 2他俩平手
        ''' </summary>
        ''' <returns>返回闲家输赢状态</returns>
        Function FailWin(ByVal _Card As String()) As String
            Dim _Res As String = ""
            Try
                Dim _Over As String = ""
                Dim _Long As String() = JudgeCardType(_Card(0)).Split("/")
                Dim _Feng As String() = JudgeCardType(_Card(1)).Split("/")
                Select Case True
                    Case Val(_Long(0)) = -1 AndAlso Val(_Feng(0)) = 5 : _Over = "0" '龙的豹子杀手碰见凤的豹子了
                    Case Val(_Long(0)) = 5 AndAlso Val(_Feng(0)) = -1 : _Over = "1" '凤的豹子杀手碰见龙的豹子了
                    Case Val(_Long(0)) > Val(_Feng(0)) : _Over = "0"
                    Case Val(_Long(0)) < Val(_Feng(0)) : _Over = "1"
                    Case Val(_Long(0)) = Val(_Feng(0)) '如果牌型相同，则比较点数
                        Select Case _Long(0)
                            Case "-1" '都是豹子杀手时是平
                                _Over = "2"
                            Case "0", "3" '都是同花或者散牌时
                                Select Case True'最大的一张比较大小
                                    Case _Long(3) > _Feng(3) '龙最大比凤最大的大
                                        _Over = "0"
                                    Case _Long(3) < _Feng(3) '龙最大比凤最大的小
                                        _Over = "1"
                                    Case _Long(3) = _Feng(3) '龙最大与凤最大相等，次最大进行比较
                                        Select Case True
                                            Case _Long(2) > _Feng(2)
                                                _Over = "0"
                                            Case _Long(2) < _Feng(2)
                                                _Over = "1"
                                            Case _Long(2) = _Feng(2) '次最大相同时
                                                Select Case True'次次最大进行比较（即最后一张小牌进行比较）
                                                    Case _Long(1) > _Feng(1)
                                                        _Over = "0"
                                                    Case _Long(1) < _Feng(1)
                                                        _Over = "1"
                                                    Case _Long(1) = _Feng(1) '平手
                                                        _Over = "2"
                                                End Select
                                        End Select
                                End Select
                            Case "1" '都是对子时
                                Select Case True
                                    Case _Long(3) > _Feng(3) '龙最大比凤最大的大
                                        _Over = "0"
                                    Case _Long(3) < _Feng(3) '龙最大比凤最大的小
                                        _Over = "1"
                                    Case _Long(3) = _Feng(3) '龙最大与凤最大相等，次最大进行比较(即单张进行比较)
                                        Select Case True
                                            Case _Long(1) > _Feng(1)
                                                _Over = "0"
                                            Case _Long(1) < _Feng(1)
                                                _Over = "1"
                                            Case _Long(1) = _Feng(1)
                                                _Over = "2"
                                        End Select
                                End Select
                            Case "2", "4" '都是顺子或者同花顺时
                                Select Case True
                                    Case _Long(3) > _Feng(3) '龙最大比凤最大的大
                                        _Over = "0"
                                    Case _Long(3) < _Feng(3)
                                        _Over = "1"
                                    Case _Long(3) = _Feng(3)
                                        _Over = "2"
                                End Select
                            Case "5" '都是豹子时
                                Select Case True
                                    Case _Long(3) > _Feng(3) '龙最大比凤最大的大
                                        _Over = "0"
                                    Case _Long(3) < _Feng(3)
                                        _Over = "1"
                                    Case _Long(3) = _Feng(3)
                                        _Over = "2"
                                End Select
                        End Select
                End Select
                _Res = _Long(0) & ":" & _Long(3) & "|" & _Feng(0) & ":" & _Feng(3) & "|" & _Over
            Catch ex As Exception
                Console.WriteLine("FailWin 方法报错 ===" & ex.Message)
            End Try
            Return _Res
        End Function

        ''' <summary>
        '''  判断3张牌的牌型 返回的数据  豹子杀手 -1/2/3/5     散牌0/1/2/4   对子1/2/3/3      顺子2/3/4/5    同花3/4/6/8     同花顺4/5/6/7    豹子5/4/4/4 
        '''  第一个数是牌型；后面的三个数是点数，点数由小到大排列，对子特殊(最后俩张牌是对子，前面是单张)
        ''' </summary>
        ''' <param name="_Card"></param>
        ''' <returns></returns>
        Public Function JudgeCardType(ByVal _Card As String) As String
            Dim _Res As String = ""
            Try
                Dim _Value As New List(Of Integer)
                Dim _iHearts, _iSpade, _iDiamonds, _iClubs As Integer
                Dim _arr() As String = _Card.Split("/")
                For k As Integer = 0 To _arr.Length - 1
                    Dim _spri = _arr(k).Substring(0, 1)
                    Select Case _spri
                        Case "1" : _iHearts += 1 '黑桃
                        Case "2" : _iSpade += 1 '红桃
                        Case "3" : _iDiamonds += 1 '梅花
                        Case "4" : _iClubs += 1 '方片
                    End Select
                    _Value.Add(_arr(k).Substring(1, 2))
                Next
                '排序
                _Value.Sort()
                Dim _IsTH As Boolean = False
                '对同花的判断 
                If _iHearts = 3 OrElse _iSpade = 3 OrElse _iDiamonds = 3 OrElse _iClubs = 3 Then
                    '同花的判断
                    _Res = "3/" & _Value(0) & "/" & _Value(1) & "/" & _Value(2)
                    _IsTH = True
                End If
                '判断是否是豹子杀手
                If _IsTH = False Then
                    If _Value(0) = 2 AndAlso _Value(1) = 3 AndAlso _Value(2) = 5 Then
                        _Res = "-1/" & _Value(0) & "/" & _Value(1) & "/" & _Value(2)
                    End If
                End If
                '判断是不是对子
                If _Value(0) = _Value(1) AndAlso _Value(1) <> _Value(2) Then
                    _Res = "1/" & _Value(2) & "/" & _Value(0) & "/" & _Value(1)
                End If
                If _Value(0) <> _Value(1) AndAlso _Value(1) = _Value(2) Then
                    _Res = "1/" & _Value(0) & "/" & _Value(1) & "/" & _Value(2)
                End If
                '对顺子的判断
                If _IsTH = False Then
                    If _Value(1) - _Value(0) = 1 AndAlso _Value(2) - _Value(1) = 1 Then
                        _Res = "2/" & _Value(0) & "/" & _Value(1) & "/" & _Value(2)
                    ElseIf _Value(0) = 2 AndAlso _Value(1) = 3 AndAlso _Value(2) = 14 Then
                        'A 2 3也是顺子，此处特殊处理  A对应的点数是14
                        _Res = "2/" & _Value(2) & "/" & _Value(0) & "/" & _Value(1)
                    End If
                End If
                '同花顺的判断
                If _IsTH = True Then
                    '对顺子的判断
                    If _Value(1) - _Value(0) = 1 AndAlso _Value(2) - _Value(1) = 1 Then
                        _Res = "4/" & _Value(0) & "/" & _Value(1) & "/" & _Value(2)
                    ElseIf _Value(0) = 2 AndAlso _Value(1) = 3 AndAlso _Value(2) = 14 Then
                        'A 2 3也是顺子，此处特殊处理  A对应的点数是14
                        _Res = "4/" & _Value(2) & "/" & _Value(0) & "/" & _Value(1)
                    End If
                End If
                '对豹子的判断
                If _Value(0) = _Value(2) Then
                    _Res = "5/" & _Value(0) & "/" & _Value(1) & "/" & _Value(2)
                End If
                '都不满足上述条件则是散牌
                If _Res = "" Then
                    _Res = "0/" & _Value(0) & "/" & _Value(1) & "/" & _Value(2)
                End If
            Catch ex As Exception
                DBErrWrite("LogicModule->JudgeCardType 判断牌型的方法报错", ex)
            End Try
            Return _Res
        End Function

        ''' <summary>
        ''' 给所有人发送数据
        ''' </summary> 
        Public Sub SendDataToAll(ByVal _Data As String)
            Console.WriteLine(_Data)
            For Each _Item In UserDic.Values
                _Item.SendData(_Data)
            Next
        End Sub
        ''' <summary>
        ''' 洗牌分牌 分2份 分别是：龙 凤  每份3张 数据格式：101/102/114
        ''' </summary>
        Public Sub WashCard()
            '生成52张牌
            Dim _Card As New List(Of String)
            For i As Integer = 2 To 14
                _Card.Add(100 + i) : _Card.Add(200 + i) : _Card.Add(300 + i) : _Card.Add(400 + i)
            Next
            '将52张牌分成2份，每份随机3张
            For j As Integer = 0 To 1
                Dim _Pin As String = ""
                Dim _Ran As Integer = 0
                For k As Integer = 1 To 3
                    _Ran = RandomInt(0, _Card.Count)
                    _Pin &= "/" & _Card(_Ran)
                    _Card.RemoveAt(_Ran)
                Next
                Card(j) = _Pin.Trim("/")
            Next
            Card(0) = "114/202/104"
            Card(1) = "214/302/205"
        End Sub

    End Class
End Module
