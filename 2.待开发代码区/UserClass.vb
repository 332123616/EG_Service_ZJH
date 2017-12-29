Imports System.Collections.Concurrent
Imports Dll_ToolModule
''' <summary>
''' 游戏服务器 用户类
''' </summary>
Public Class UserClass
#Region "### 禁止修改代码，如果使用过程中不方便，在反馈给孙哥 ###"
    ''' <summary>
    ''' 校验码
    ''' </summary>
    Public CheckCode As String
    ''' <summary>
    ''' 分区号
    ''' </summary>
    Public Zone As Integer
    ''' <summary>
    ''' 座位号
    ''' </summary>
    Public Seat As Integer
    ''' <summary>
    ''' 游戏帐号
    ''' </summary>
    Public Account As String
    ''' <summary>
    ''' 游戏积分
    ''' </summary>
    Public Integral As Double = 10000
    ''' <summary>
    ''' 活跃时间
    ''' </summary>
    Public ActiveTime As DateTime = Now
    ''' <summary>
    ''' 验证次数
    ''' </summary>
    Public HeartTimes As Integer = 0
    ''' <summary>
    ''' 当前连接状态
    ''' </summary>
    Public OnLine As Boolean = True
    ''' <summary>
    ''' 是否已玩
    ''' </summary>
    Public IsPlay As Boolean = False
    ''' <summary>
    ''' 信息序号集合 用于过滤重复信息
    ''' </summary>
    Public MsgNumArray As New ArrayList
    ''' <summary>
    ''' 保存socket key：转接器标记  value:socket
    ''' </summary>
    Public SocketDic As New ConcurrentDictionary(Of String, ServerUserClass)
    ''' <summary>
    ''' 想客户端发送信息的序号
    ''' </summary>
    Private MsgNum As Long = 0
    ''' <summary>
    '''信息过滤锁
    ''' </summary>
    Public FilterLock As New Object
    ''' <summary>
    ''' 经销商序号
    ''' </summary>
    Public Dealer As Integer
    ''' <summary>
    ''' 分站序号
    ''' </summary>
    Public Substation As Integer
    ''' <summary>
    ''' 代理
    ''' </summary>
    Public Agent As String
    ''' <summary>
    ''' 昵称
    ''' </summary>
    Public Nick As String = ""
    ''' <summary>
    ''' 玩家登入方式
    ''' </summary>
    Public Device As String = ""
    ''' <summary>
    '''会员等级
    ''' </summary>
    Public UserRank As String = "1"
    ''' <summary>
    ''' 账号类型
    ''' </summary>
    Public AccountType As String = ""
    ''' <summary>
    ''' 给中心服务器发送的注单类型
    ''' </summary>
    Public NoteType As String
    ''' <summary>
    ''' 发送数据
    ''' </summary>
    ''' <param name="_MsgStr"></param>
    Public Sub SendData(ByVal _MsgStr As String)
        MsgNum += 1 '消息序号自增加
        Dim _Key As String = CheckCode & MsgNum '生成消息序列KEY值
        _MsgStr = "ZF>" & EGTag & ">" & CheckCode & ">" & MsgNum & ">" & _MsgStr  '重组发送数据
        MsgList.TryAdd(_Key, New MsgClass(Me, _Key, _MsgStr)) '将数据加入消息序列中
    End Sub

    ''' <summary>
    ''' 发送数据
    ''' </summary>
    ''' <param name="_MsgStr"></param>
    Public Sub SendData1(ByVal _MsgStr As String)
        If OnLine = True Then
            For Each _Item In SocketDic.Values
                If _Item IsNot Nothing Then
                    _Item.SendAppData(_MsgStr)
                End If
            Next
        End If
    End Sub

    ''' <summary>
    ''' 如果类中有使用非托管对象，那么当本类销毁的时候，在销毁过程中加入该对象
    ''' </summary>
    Public Sub Dispose()
        For Each item In SocketDic.Values
            If item IsNot Nothing Then
                item = Nothing
            End If
        Next
    End Sub
    ''' <summary>
    ''' 向中心服务器发送数据
    ''' </summary>
    Public Sub SendDataToCenter(ByVal _Data As String)
        ManageServer.LoginServiceSocket.SendAppData(_Data)
    End Sub
#End Region

#Region "###  根据游戏不同在下方添加所需变量 ###"
    ''' <summary>
    ''' 下注格式： 0/0/0/0/0/0/0     龙/凤/8对以上/顺子/同花/同花顺/豹子
    ''' </summary>
    Public Bet As String = "0/0/0/0/0/0/0"
    ''' <summary>
    ''' 中奖分数
    ''' </summary>
    Public Score As Double
    ''' <summary>
    ''' 反水
    ''' </summary>
    Public BackWater As Integer
    ''' <summary>
    ''' 注单内容
    ''' </summary>
    Public NoteContent As String
#End Region

End Class
