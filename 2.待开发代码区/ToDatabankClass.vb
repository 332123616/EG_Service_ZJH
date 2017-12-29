Imports System.Data.SqlClient
''' <summary>
''' 对数据库进行操作的函数集合类  DBControl
''' </summary>
Public Class ToDatabankClass
#Region "###  根据游戏不同在下方添加数据库操作过程或函数 ###"
    Private ConfigTable As String = "T_Game_Config_7PK"
    Private IndexTable As String = "T_Game_Index_7PK"
    Private AfficheModelTable As String = "T_Game_AfficheModel"
    Private AfficheTable As String = "T_Game_Affiche"


    ''' <summary>
    ''' 插入中奖信息
    ''' </summary>
    ''' <param name="_Dealer">经销商序号</param>
    ''' <param name="_Subregion">分站序号</param>
    ''' <param name="_GameMark">游戏标记</param>
    ''' <param name="_Seat">座位号</param>
    ''' <param name="_WinName">中奖标记</param>
    ''' <param name="_Account">账号</param>
    ''' <param name="_Nick">昵称</param>
    ''' <param name="_Module">模组号</param>
    ''' <param name="_Bet">下注多少</param>
    ''' <param name="_SendJackpot">发放多少彩金(赠分)</param>
    ''' <param name="_AlreadySend">是否发放过(0未发放 1已发放)</param>
    ''' <param name="_WinState">中奖状态(0自然 1强增 2取消强增)</param>
    ''' <param name="_WinType">中奖类型(0彩金记录表 1是奖项中奖记录)</param>
    ''' <param name="_MemberType">会员类型(0会员 1是NPC)</param>
    ''' <param name="_Fullbets">是否满投注（0未满 1满）</param>
    Sub InsertPrizeInfo(_Dealer As Integer, _Subregion As Integer, _GameMark As String, _Seat As Integer, _WinName As String, _Account As String, _Nick As String, _Module As Integer, _Bet As Double, _SendJackpot As Double, _AlreadySend As Integer, _WinState As Integer, _WinType As Integer, _MemberType As Integer, _Fullbets As Integer, _AddScore As Double, _JackpotPoint As Double)
        Dim sql As String = "insert into T_Record_WinRecord(_iDealer,_iSubregion,_dComplimentDatetime,_vGameMark,_iNumber,_vWinName,_vSendAssociator,_nNick,_iModule,_fBetAmount,_fSendJackpotAmount,_iAlreadySend,_iWinState,_dSendDatetime,_iWinType,_iMemberType,_iFullbets,_fAddScore,_fJackpotPoint)values(@_iDealer,@_iSubregion,@_dComplimentDatetime,@_vGameMark,@_iNumber,@_vWinName,@_vSendAssociator,@_nNick,@_iModule,@_fBetAmount,@_fSendJackpotAmount,@_iAlreadySend,@_iWinState,@_dSendDatetime,@_iWinType,@_iMemberType,@_iFullbets,@_fAddScore,@_fJackpotPoint)"
        Dim sqlparams() As SqlParameter = New SqlParameter() {
               New SqlParameter("@_iDealer", _Dealer),
               New SqlParameter("@_iSubregion", _Subregion),
               New SqlParameter("@_dComplimentDatetime", Now),
               New SqlParameter("@_vGameMark", _GameMark),
               New SqlParameter("@_iNumber", _Seat),
               New SqlParameter("@_vWinName", _WinName),
               New SqlParameter("@_vSendAssociator", _Account),
               New SqlParameter("@_nNick", _Nick),
               New SqlParameter("@_iModule", _Module),
               New SqlParameter("@_fBetAmount", _Bet),
               New SqlParameter("@_fSendJackpotAmount", _SendJackpot),
               New SqlParameter("@_iAlreadySend", _AlreadySend),
               New SqlParameter("@_iWinState", _WinState),
               New SqlParameter("@_dSendDatetime", Now),
               New SqlParameter("@_iWinType", _WinType),
               New SqlParameter("@_iMemberType", _MemberType),
               New SqlParameter("@_iFullbets", _Fullbets),
               New SqlParameter("@_fAddScore", _AddScore),
                New SqlParameter("@_fJackpotPoint", _JackpotPoint)
            }
        DBControl.ExecParamSQL(3, sql, sqlparams)
    End Sub
    ''' <summary>
    ''' 根据玩家等级、分站、经销商、游戏标记、 获取返水比例
    ''' </summary>
    ''' <returns></returns>
    Public Function UserRank() As DataTable
        Return DBControl.ExecParamSQL(1, "select _fRepayRates,_iDealer,_iSubregion,_iRank from  T_Game_RepaySet  WITH(NOLOCK) where  _vGameMark='" & EGTag & "'")
    End Function
    ''' <summary>
    ''' 查询跑马灯模板
    ''' </summary>
    ''' <param name="_Condition">赢的分数</param>
    ''' <param name="_GameMark">游戏标记 中风火轮和彩金时传空字符串</param>
    ''' <param name="_PrizeName">中奖类型 如:DRZT</param>
    ''' <returns></returns>
    Public Function SelectAfficheModel(ByVal _GameMark As String, ByVal _PrizeName As String, ByVal _Condition As String) As String

        Dim sql As String = "Select top 1 _nContent from " & AfficheModelTable & "  With(NOLOCK) where _iSwitch=1 And _iCondition<=@_Condition And _vGameMark=@_GameMark And _vPrizeName=@_PrizeName "
        Dim sqlparams() As SqlParameter = New SqlParameter() {
                New SqlParameter("@_Condition", _Condition),'得分需要大于最低限制分数
                New SqlParameter("@_GameMark", _GameMark),'中风火轮和彩金时传空字符串
                New SqlParameter("@_PrizeName", _PrizeName)'中奖类型 FG_3 BG_6 Mini Mega Grand LG
        }
        Return DBControl.ExecParamSQL(2, sql, sqlparams)
    End Function

    ''' <summary>
    ''' 添加跑马灯
    ''' </summary>
    ''' <param name="_nContent">跑马灯内容</param>
    ''' <param name="_GameIndication">跑马灯显示范围 添加游戏标记，如果是风火轮和彩金写九个游戏的标记用逗号隔开</param>
    Public Function InsertAffiche(ByVal _nContent As String, ByVal _GameIndication As String)
        Dim sql As String = "insert  into " & AfficheTable & "(_dStartTime,_nContent,_dEndTime,_iLanguage,_vGameIndication,_iSwitch,_vAdmin,_iAfficheType,_iShowIndication)values(getdate(),@_nContent,@_dEndTime,0,@_GameIndication,1,'',3,1)"
        Dim sqlparams() As SqlParameter = New SqlParameter() {
                New SqlParameter("@_nContent", _nContent),'跑马灯信息
                New SqlParameter("@_dEndTime", DateAdd(DateInterval.Minute, 5, Now)),'可以显示的结束时间
                New SqlParameter("@_GameIndication", _GameIndication)'游戏标记，彩金和风火轮是九个游戏的所有标记，其他的写本游戏
        }
        Return DBControl.ExecParamSQL(3, sql, sqlparams)
    End Function

    ''' <summary>
    '''查询玩家的模组数据
    ''' </summary>
    ''' <returns></returns>
    Public Function SelectUserModule(ByVal _UserName As String) As DataTable
        Return DBControl.ExecParamSQL(1, "select _iModule,_iDealer,_iSubregion,_vUserName  from T_Game_MemberControl  WITH(NOLOCK) where  _vUserName in (" & _UserName & ") and  _vGameMark='" & EGTag & "'")
    End Function
    ''' <summary>
    ''' 查询机台座位信息
    ''' </summary>
    ''' <returns></returns>
    Function SelectRoomSeat() As String
        Return DBControl.ExecParamSQL(2, "select _vRoomSeat from T_Game_ProgramConfig WITH(NOLOCK) where _vTag='" & EGTag & "'")
    End Function
    ''' <summary>
    ''' 查询Npc用户名
    ''' </summary>
    ''' <returns></returns>
    Function SelectNpcInfo() As DataTable
        Return DBControl.ExecParamSQL(1, "select top 1 _vUserName,_nNick,_dRegisterTime,_iDealer,_iSubregion from T_Game_NPC WITH(NOLOCK) where _iWinState=0")
    End Function
#End Region

End Class
