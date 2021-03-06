﻿Imports System.Data.Entity
Imports System.IO
Imports SupraReports.Model

Public Class FormPrincipal
  Private titulos As IEnumerable(Of String) =
    New List(Of String)() From {
      "Nice and Easy System for Supra Info Extraction",
      "Navegador del Entorno Supra con Salida de Informes Excel",
      "Nuevo Editor Sql de Supra con Informes Excel"
    }
  Private db As SupraReportsContext
  Private informe As Informe
  Private horaComienzoLanzarInformes As Date

  Private Sub FormPrincipal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    Randomize()
    Text = titulos(Math.Floor((titulos.Count) * Rnd()))

    db = New SupraReportsContext()
    CargarInformes()
  End Sub

  Private Sub CbInforme_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CbInforme.SelectedIndexChanged
    If Not CbInforme.Focused OrElse ValidarCambioInforme() Then
      PnlEditar.Controls.Clear()
      If CbInforme.SelectedItem IsNot Nothing Then
        informe = CbInforme.SelectedItem
        DescartarCambios()
        CargarConsultas()
      End If
      EstablecerEstadoBotones()
    End If
  End Sub

  Private Sub BtnNuevo_Click(sender As Object, e As EventArgs) Handles BtnNuevo.Click
    If ValidarCambioInforme() Then
      Dim nuevoInformeDialog As FormNuevoInforme = New FormNuevoInforme()
      If nuevoInformeDialog.ShowDialog(Me) = DialogResult.OK Then

        DescartarCambios()
        informe = Informe.Crear(nuevoInformeDialog.TbNombre.Text, Environment.UserName)
        informe.AnadirConsulta(Consulta.Crear(String.Empty, String.Empty))
        db.Informes.Add(informe)
        db.SaveChanges()

        CargarInformes()
        CargarConsultas()
        EstablecerEstadoBotones()
      End If
    End If
  End Sub

  Private Sub BtnGuardar_Click(sender As Object, e As EventArgs) Handles BtnGuardar.Click
    db.SaveChanges()
  End Sub

  Private Sub BtnGuardarComo_Click(sender As Object, e As EventArgs) Handles BtnGuardarComo.Click
    If ValidarCambioInforme() Then
      Dim nuevoInformeDialog As FormNuevoInforme = New FormNuevoInforme()

      If nuevoInformeDialog.ShowDialog(Me) = DialogResult.OK Then
        Dim nuevoInforme = Informe.Copiar(informe)
        nuevoInforme.Nombre = nuevoInformeDialog.TbNombre.Text

        informe = nuevoInforme

        DescartarCambios()
        db.Informes.Add(informe)
        db.SaveChanges()

        CargarInformes()
        CargarConsultas()
        EstablecerEstadoBotones()
      End If
    End If
  End Sub

  Private Sub BtnEliminarInforme_Click(sender As Object, e As EventArgs) Handles BtnEliminarInforme.Click
    If MessageBox.Show(Me, "¿Desea eliminar el informe?", "Confirmar eliminación", MessageBoxButtons.YesNo) = DialogResult.Yes Then
      db.Programaciones.Remove(informe.Programacion)
      db.Informes.Remove(informe)
      db.SaveChanges()

      informe = Nothing
      CargarInformes()
      CargarConsultas()
      EstablecerEstadoBotones()
    End If
  End Sub

  Private Sub BtnAnadirConsulta_Click(sender As Object, e As EventArgs) Handles BtnAnadirConsulta.Click
    Dim consulta As Consulta = Consulta.Crear(String.Empty, String.Empty)
    informe.AnadirConsulta(consulta)

    Dim control As EditarConsultaUserControl = New EditarConsultaUserControl(db, consulta)
    PnlEditar.Controls.Add(control)
    PnlEditar.ScrollControlIntoView(control)
  End Sub

  Private Sub BtnEjecutar_Click(sender As Object, e As EventArgs) Handles BtnEjecutar.Click
    EjecutarInforme(informe, False)
  End Sub

  Private Sub BtnProgramar_Click(sender As Object, e As EventArgs) Handles BtnProgramar.Click
    Dim formProgramaciones As FormProgramaciones = New FormProgramaciones()
    formProgramaciones.ShowDialog(Me)
  End Sub

  Private Sub BtnConfiguracion_Click(sender As Object, e As EventArgs) Handles BtnConfiguracion.Click
    Dim formConfiguracion As FormConfiguracion = New FormConfiguracion()
    formConfiguracion.ShowDialog(Me)
  End Sub

  Private Sub BtnEjecutarProgramaciones_Click(sender As Object, e As EventArgs) Handles BtnEjecutarProgramaciones.Click
    MinimizarEnAreaNotificacion()
    horaComienzoLanzarInformes = DateTime.Now
    LanzarProgramacionesDe(horaComienzoLanzarInformes)
    TimerSegundo.Start()
  End Sub

  Private Sub TimerSegundo_Tick(sender As Object, e As EventArgs) Handles TimerSegundo.Tick
    Dim horaEjecucion = DateTime.Now
    If horaEjecucion.Second = 0 Then
      Console.WriteLine("Tick: {0}", horaEjecucion.ToLongTimeString)
      LanzarProgramacionesDe(horaEjecucion)
    End If
  End Sub

  Private Sub FormPrincipal_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
    If Not ValidarCambioInforme() Then
      e.Cancel = True
    End If
  End Sub

  Private Sub MenuIconoNotificacionCancelarProgramaciones_Click(sender As Object, e As EventArgs) Handles MenuIconoNotificacionCancelarProgramaciones.Click
    Show()
    IconoNotificacion.Visible = False
    TimerSegundo.Stop()

    Dim formEjecuciones As FormResumenEjecuciones = New FormResumenEjecuciones(horaComienzoLanzarInformes)
    formEjecuciones.ShowDialog(Me)
  End Sub

  Private Sub PnlEditar_Resize(sender As Object, e As EventArgs) Handles PnlEditar.Resize
    BtnAnadirConsulta.Location = New Point(BtnAnadirConsulta.Location.X, PnlEditar.Location.Y + PnlEditar.Size.Height + 4)
  End Sub

  Private Sub CargarInformes()
    InformeBindingSource.DataSource = db.Informes.Local.ToBindingList()
    db.Informes.Load()
    CbInforme.SelectedItem = informe
    If informe Is Nothing Then
      CbInforme.Text = String.Empty
    End If
  End Sub

  Private Sub CargarConsultas()
    PnlEditar.Controls.Clear()
    If informe IsNot Nothing Then
      For Each consulta In informe.Consultas
        Dim control As EditarConsultaUserControl = New EditarConsultaUserControl(db, consulta)
        PnlEditar.Controls.Add(control)
      Next
    End If
  End Sub

  Private Sub LanzarProgramacionesDe(horaEjecucion As Date)
    Dim programaciones As IEnumerable(Of Programacion)
    Using db = New SupraReportsContext()
      programaciones = db.Programaciones.Include("Informe.Consultas.Parametros").AsNoTracking().ToList()
    End Using
    For Each p In programaciones.AsParallel()
      If p.ObtenerDiasProgramados().Contains(horaEjecucion.DayOfWeek) AndAlso
        p.ObtenerHoraProgramada() = horaEjecucion.Hour AndAlso
        p.ObtenerMinutoProgramado() = horaEjecucion.Minute Then
        EjecutarInforme(p.Informe, True)
      End If
    Next
  End Sub

  Private Sub EjecutarInforme(informe As Informe, guardarEjecucion As Boolean)
    Dim outputFile As String = ComponerRutaSalidaInforme(informe)
    Dim erroresInforme As List(Of String) = New List(Of String)()
    Using excelBuilder = New ExcelBuilder(outputFile)
      For Each consulta In informe.Consultas.Where(Function(x) x.Habilitada)
        Using dao = New GeneralDao(GeneralDao.CrearConexionMySql())
          Dim contents As IDataReader = Nothing
          Try
            contents = dao.EjecutarSelect(consulta.ComponerSqlResultado())
            excelBuilder.AddWorksheet(consulta.Nombre, contents)
          Catch ex As Exception
            excelBuilder.AddWorksheet(consulta.Nombre, {ex})
          End Try
        End Using
      Next
      Try
        excelBuilder.Build()
      Catch ex As Exception
        erroresInforme.Add(ex.Message)
      End Try
    End Using
    If guardarEjecucion Then
      db.Informes.Find(informe.Id).AnadirEjecucion(informe.Programacion.Hora, String.Join(" - ", erroresInforme), outputFile)
      db.SaveChanges()
    End If
  End Sub

  Private Shared Function ComponerRutaSalidaInforme(informe As Informe) As String
    Dim folderPath = My.Settings.RutaDirectorioSalida
    If String.IsNullOrEmpty(folderPath) Then folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
    Dim fileName As String = String.Format("{0}_{1}.xlsx", informe.Nombre.Replace(" ", "_"), DateTime.Now.ToString("yyyyMMdd_HHmmss"))
    Dim outputFile As String = Path.Combine(folderPath, fileName)
    Return outputFile
  End Function

  Private Sub EstablecerEstadoBotones()
    Dim habilitado As Boolean = informe IsNot Nothing
    BtnGuardar.Enabled = habilitado
    BtnGuardarComo.Enabled = habilitado
    BtnEliminarInforme.Enabled = habilitado
    BtnEjecutar.Enabled = habilitado
    BtnAnadirConsulta.Enabled = habilitado
  End Sub

  Private Sub MinimizarEnAreaNotificacion()
    IconoNotificacion.Visible = True
    IconoNotificacion.ShowBalloonTip(1000)
    Hide()
  End Sub

  Private Function ValidarCambioInforme() As Boolean
    If informe IsNot Nothing AndAlso HayCambiosSinGuardar() Then
      Dim resultado = MessageBox.Show("¿Desea guardar los cambios?", "Cambios en el informe", MessageBoxButtons.YesNoCancel)
      Select Case resultado
        Case DialogResult.Yes
          db.SaveChanges()
          Return True
        Case DialogResult.No
          DescartarCambios()
          Return True
        Case DialogResult.Cancel
          Return False
      End Select
    End If
    Return True
  End Function

  Private Sub DescartarCambios()
    If db IsNot Nothing Then db.Dispose()
    db = New SupraReportsContext()
    If informe IsNot Nothing Then db.Informes.Attach(informe)
  End Sub

  Private Function HayCambiosSinGuardar() As Boolean
    Return db.ChangeTracker.HasChanges
  End Function
End Class
