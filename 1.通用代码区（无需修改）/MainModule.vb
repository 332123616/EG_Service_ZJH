Imports System.Collections.Concurrent
Imports Dll_ToolModule
Imports System.Data.SqlClient
''' <summary>
''' 程序名称：游戏服务 框架代码
''' 程序作用：
''' 1、暂无
''' 更新记录：
''' 2016/11/15 修改人：阿森 修改内容：开发
''' 2017/08/30 修改人：孙哥 修改内容：重构开发。
''' </summary>
Module MainModule
    ''' <summary>
    ''' 游戏服务 唯一标记(每个游戏必须不同)
    ''' </summary>
    Public Const EGTag As String = "7PK"

    ''' <summary>
    ''' 维护开关 false非维护 true维护中
    ''' </summary>
    Public Maintenance As Boolean
    ''' <summary>
    ''' 连接数据库字符串
    ''' </summary>
    Private ConnectString As String = ""
    ''' <summary>
    ''' 线程更新本服务活跃
    ''' </summary>
    Private UpActiveTimeTh As Threading.Thread
    ''' <summary>
    ''' 数据库操作对象
    ''' </summary>
    Public DBControl As SQLServerControlClass = Nothing
    ''' <summary>
    ''' XML信息读取类{Active 表示活跃数据库标记 ConnectA数据库A连接字符串 ConnectB数据库B连接字符串}
    ''' </summary>
    Private WithEvents XMLLoad As Dll_ToolModule.XMLFileLoadClass
    ''' <summary>
    ''' 存储数据库错误记录数量，此数量如果大于1000那么将不存储数据库
    ''' </summary>
    Private ToDBMaxErrCount As Integer = 0
    ''' <summary>
    ''' 存储数据库过程记录数量，此数量如果大于1000那么将不存储数据库
    ''' </summary>
    Private ToDBMaxSubCount As Integer = 0

    ''' <summary>
    ''' XML文件信息被修改后自动触发
    ''' </summary>
    Private Sub XMLChangeEvent() Handles XMLLoad.XMLDataValueChangeEvent
        'XML变化有可能更换数据库连接，所以重新调用实例化判断方法
        DBUpdateControl()
    End Sub
    ''' <summary>
    ''' 实例化对象
    ''' </summary>
    Private Sub DBUpdateControl()
        Dim _TmpConnectString As String = ""
        '判断XML记录的活跃数据库标记，根据活跃标记来区分使用那个数据库连接字符串
        Select Case XMLLoad.GetValue("Active")
            Case "A" : _TmpConnectString = Decryption(XMLLoad.GetValue("ConnectA"))
            Case "B" : _TmpConnectString = Decryption(XMLLoad.GetValue("ConnectB"))
        End Select
        '判断数据库连接字符串是否有变化，如果有，重新建立数据库对象
        If _TmpConnectString <> ConnectString Then
            ConnectString = _TmpConnectString
            If DBControl IsNot Nothing Then DBControl.Dispose()
            DBControl = New SQLServerControlClass(ConnectString)
        End If
    End Sub



    ''' <summary>
    ''' 
    ''' </summary>
    Public ManageDB As ToDatabankClass
    ''' <summary>
    ''' 管理APP连接的对象
    ''' </summary>
    Public ManageClient As ToListenClientClass
    ''' <summary>
    ''' 管理连接游戏服务器的对象
    ''' </summary>
    Public ManageServer As ToLoginServerClass
    ''' <summary>
    ''' 所有玩家集合Key记录校验码
    ''' </summary>
    Public UserList As New ConcurrentDictionary(Of String, UserClass)
    ''' <summary>
    ''' 消息序列
    ''' </summary>
    Public MsgList As New ConcurrentDictionary(Of String, MsgClass)


    ''' <summary>
    ''' 启动服务
    ''' </summary>
    Public Sub StartService()
        Try
            Dll_ToolModule.OnlyFileName = EGTag '设置LOG文件名唯一标记
            'LogWrite(EGTag & " 服务启动")
            XMLLoad = New Dll_ToolModule.XMLFileLoadClass("Config.xml", {"Active", "ConnectA", "ConnectB"})
            '实例化数据库操作对象
            DBUpdateControl()
            DBSubWrite(EGTag & " 服务启动")

            '读取数据库中配置的服务监听IP和端口 以及服务是否维护
            Dim _Tables As DataTable = DBControl.ExecParamSQL(1, "select _vListenIP,_iListenPort,_iListenMaxCount,_iMaintenance from T_Server_ServiceManage with (NOLOCK) where _vTag='" & EGTag & "'")
            Maintenance = _Tables.Rows(0)("_iMaintenance") '存储服务是否维护中
            '建立监听连接对象
            ManageClient = New ToListenClientClass(_Tables.Rows(0)("_vListenIP"), _Tables.Rows(0)("_iListenPort"), _Tables.Rows(0)("_iListenMaxCount"))
            _Tables.Clear() : _Tables.Dispose()
            '读取数据库中连接登入服务器的IP和端口
            _Tables = DBControl.ExecParamSQL(1, "select _vConnectIP,_iConnectPort from T_Server_ServiceManage with (NOLOCK) where _vTag='DR'")
            '建立连接登入服务器对象
            ManageServer = New ToLoginServerClass(_Tables.Rows(0)("_vConnectIP"), _Tables.Rows(0)("_iConnectPort"))
            _Tables.Clear() : _Tables.Dispose()
            '创建线程更新本服务活跃
            UpActiveTimeTh = New Threading.Thread(AddressOf UpActiveTime)
            UpActiveTimeTh.Start()
            ManageDB = New ToDatabankClass
            ItemInitialise() '初始化事件
        Catch ex As Exception
            LogWrite(EGTag & "服务器(重要错误)服务启动报错", ex)
        End Try
    End Sub
    ''' <summary>
    ''' 停止服务
    ''' </summary>
    Public Sub StopService()
        Try
            'LogWrite(EGTag & " 服务停止")
            DBSubWrite(EGTag & " 服务停止")
            Dll_ToolModule.LogIfWrite = False '设置之后不再写入错误记录
            XMLLoad.Dispose()
            DBControl.Dispose()
            ManageClient.Dispose()
            ManageServer.Dispose()
            UpActiveTimeTh.Abort()
        Catch ex As Exception
            LogWrite(EGTag & "服务器(重要错误)服务停止报错", ex)
        End Try
    End Sub


    ''' <summary>
    ''' 更新服务活跃时间
    ''' </summary>
    Private Sub UpActiveTime()
        Do While True
            DBControl.ExecParamSQL(3， "update T_Server_ServiceManage Set _dCheckActiveTime=getdate() where _vTag='" & EGTag & "'")
            Threading.Thread.Sleep(3000)
            Loop
    End Sub

    ''' <summary>
    ''' 记录过程到数据库
    ''' </summary>
    ''' <param name="_Content">记录信息</param>
    Public Sub DBSubWrite(ByVal _Content As String)
        If Dll_ToolModule.LogIfWrite = False Then Exit Sub
        ToDBMaxSubCount += 1
        Dim sqlparams() As SqlParameter = New SqlParameter() {New SqlParameter("@_nContent", _Content)}
        If ToDBMaxSubCount > 1000 OrElse DBControl.ExecParamSQL(3, "insert into T_Server_ServiceLog (_vTag,_nContent,_dTime,_iType)values('" & EGTag & "',@_nContent,getdate(),1)", sqlparams) = 0 Then
            LogWrite(EGTag & "服务器(重要错误)存储数据库失败 记录" & _Content)
        End If
    End Sub

    ''' <summary>
    ''' 记录错误到数据库
    ''' </summary>
    ''' <param name="_title"></param>
    ''' <param name="_ex"></param>
    Public Sub DBErrWrite(ByVal _title As String, ByVal _ex As Exception)
        If Dll_ToolModule.LogIfWrite = False Then Exit Sub
        ToDBMaxErrCount += 1
        Dim _Tmp As String = _title
        If _ex IsNot Nothing Then _Tmp &= vbCrLf & _ex.Message & vbCrLf & _ex.StackTrace
        Dim sqlparams() As SqlParameter = New SqlParameter() {New SqlParameter("@_nContent", _Tmp)}
        If ToDBMaxErrCount > 1000 OrElse DBControl.ExecParamSQL(3, "insert into T_Server_ServiceLog (_vTag,_nContent,_dTime,_iType)values('" & EGTag & "',@_nContent,getdate(),0)", sqlparams) = 0 Then
            LogWrite(EGTag & "服务器(重要错误)存储数据库失败 记录" & _title, _ex)
        End If
    End Sub

End Module
