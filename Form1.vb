
Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckForIllegalCrossThreadCalls = False '我知道其中的风险，只是这个程序不需要那么多花里胡哨的东西
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        MsgBox("请牢记随机出来的密码！", MsgBoxStyle.Exclamation, "注意！")
        Randomize(System.DateTime.Now.Ticks)
        TextBox2.Text = Math.Round(Rnd() * 89999999999 + 1000000000)
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox1_DragDrop(sender As Object, e As DragEventArgs) Handles ListBox1.DragDrop
        If e.Data.GetDataPresent(DataFormats.FileDrop, False) Then
            Dim dragedfiles As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
            For Each s As String In dragedfiles
                ListBox1.Items.Add(s)
            Next
        End If
    End Sub

    Private Sub ListBox1_DragEnter(sender As Object, e As DragEventArgs) Handles ListBox1.DragEnter
        e.Effect = DragDropEffects.All
    End Sub

    Private Sub ListBox1_DoubleClick(sender As Object, e As EventArgs) Handles ListBox1.DoubleClick
        Try
            ListBox1.Items.RemoveAt(ListBox1.SelectedIndex)
        Catch
        End Try
    End Sub

    Private Sub ListBox1_MouseUp(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseUp
        Timer1.Enabled = True '兜一圈子就是为了双击清除列表
        'Debug.WriteLine(listrightdulclick)
        If e.Button = MouseButtons.Right And listrightdulclick > 0 Then
            ListBox1.Items.Clear()
        ElseIf e.Button <> MouseButtons.Right Then
            Timer1.Enabled = False
            listrightdulclick = 0
        End If
    End Sub
    Dim listrightdulclick As Byte = 0

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        listrightdulclick += 1 'vb.net没有右键双击事件？？？
        ' Debug.WriteLine(listrightdulclick)
        If listrightdulclick >= 5 Then
            listrightdulclick = 0
            Timer1.Enabled = False
        End If
    End Sub
    ''' <summary>
    ''' 对文件进行处理和调用加密
    ''' </summary>
    ''' <param name="config"> 'filelocal As String, filesave As String, password As String</param>
    Private Sub Mainwork(config As Object())
        Try
            Dim fsave As New IO.FileStream(config(1), IO.FileMode.Create) '备写文件
            Dim all_byte As Byte() = IO.File.ReadAllBytes(config(0)) '读,并且自己用来加密
            Dim byenkey As Byte '不并行的加密
            Dim passwordbyt = System.Text.Encoding.Unicode.GetBytes(config(2)), passwordbyte_count As Integer, password_now_byte As Integer = 0 '密码大于一个字节的话，得换着字节去对应文件字节加密
            passwordbyte_count = passwordbyt.Length - 1
            If all_byte.Length > 1024000 Then '过于小的文件反而会在分段、调度线程等问题上浪费开销。暂时设置成1MB
                Dim muti_process = Threading.Tasks.Parallel.For(0, all_byte.Length - 1, Sub(i)
                                                                                            'Debug.WriteLine(password_now_byte)
                                                                                            all_byte(i) = BitConverter.GetBytes(all_byte(i) Xor passwordbyt(Math.Round(password_now_byte / (all_byte.Length - 1) * i)))(0) 'XOR加密/解密操作，加转换防止溢出BUG,这里还有个特别愚蠢的操作
                                                                                            '有什么办法比如100个字节文件，5个字节的密码，然后文件字节五个一组循环使用密码呢？以下操作会在多线程加密中造成密码数组读取溢出，原因未知
                                                                                            '如果按上面我的操作，等于是每个密码的字节加密的文件堆在一块，这样会不会增加逆向破解的概率？
                                                                                            'If password_now_byte < passwordbyte_count Then
                                                                                            ' password_now_byte += 1
                                                                                            'Else
                                                                                            ' password_now_byte = 0
                                                                                            'End If
                                                                                            '-==-=-==--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
                                                                                        End Sub)
                Dim timer_finish As Threading.Timer = New Threading.Timer(New Threading.TimerCallback(Sub() '判断是否处理完成，上面的是多线程的
                                                                                                          If muti_process.IsCompleted = True Then
                                                                                                              Threading.ThreadPool.QueueUserWorkItem(Sub()
                                                                                                                                                         IO.File.WriteAllBytes(config(1), all_byte)
                                                                                                                                                     End Sub)
                                                                                                              timer_finish.Dispose()
                                                                                                          End If
                                                                                                      End Sub), "timer_finish", 0, 100)
                'fsave.Write(all_byte, 0, all_byte.Length)
            Else
                For i = 0 To all_byte.Length - 1
                    byenkey = BitConverter.GetBytes(all_byte(i) Xor passwordbyt(password_now_byte))(0) 'XOR加密/解密操作，加转换防止溢出BUG
                    If password_now_byte < passwordbyte_count Then
                        password_now_byte += 1
                    Else
                        password_now_byte = 0
                    End If
                    fsave.WriteByte(byenkey)
                Next
            End If
            fsave.Close()
            fsave.Dispose()
            ProgressBar1.Value += 1
        Catch e As Exception
            Debug.WriteLine(e.ToString)
            Exit Sub
        End Try
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Button2.Enabled = False : Button3.Enabled = False : ListBox1.Enabled = False
        If TextBox2.Text = "" Then
            MsgBox("请输入密码", MsgBoxStyle.Critical, "没得密码？？！")
            Exit Sub
        End If
        Dim xorfile As String '生成的加密文件目录地址
        If ListBox1.Items.Count > 0 Then
            ProgressBar1.Value = 0
            ProgressBar1.Maximum = ListBox1.Items.Count
            For Each files In ListBox1.Items
                If InStr(IO.Path.GetFileName(files), "(_Xor_)", CompareMethod.Text) = 0 Then '添加或删去加密标记
                    xorfile = IO.Path.GetDirectoryName(files) & "\(_Xor_)" & IO.Path.GetFileName(files)
                Else
                    xorfile = Replace(files.ToString, "(_Xor_)", "",,, CompareMethod.Text)
                End If
                Dim config As Object() = New Object(2) {files, xorfile, TextBox2.Text}
                Threading.ThreadPool.QueueUserWorkItem(AddressOf Mainwork, config)
            Next
        End If
        '判断任务是否完成
        Dim timer_finish As Threading.Timer = New Threading.Timer(New Threading.TimerCallback(Sub()
                                                                                                  If ProgressBar1.Value = ProgressBar1.Maximum Then
                                                                                                      APIBeep(262, 100)
                                                                                                      APIBeep(330, 100)
                                                                                                      APIBeep(392, 100)
                                                                                                      APIBeep(494, 150)
                                                                                                      Button2.Enabled = True : Button3.Enabled = True : ListBox1.Enabled = True
                                                                                                      timer_finish.Dispose()
                                                                                                  End If
                                                                                              End Sub), "timer_finish", 0, 500)
    End Sub
    Private Declare Function APIBeep Lib "kernel32" Alias "Beep" (ByVal dwFreq As Long, ByVal dwDuration As Long) As Long
End Class

