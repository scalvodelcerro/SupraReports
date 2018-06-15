﻿Imports System.Text.RegularExpressions
Imports SupraReports.Model

Public Class EditarConsultaUserControl
  Private Const PatternParametro As String = "#(\w+)#"
  Private colorResaltarTexto As Color = Color.PaleGreen

  Public Sub New(consulta As Consulta)
    InitializeComponent()
    Me.Consulta = consulta
  End Sub

  Public WriteOnly Property Consulta As Consulta
    Set(value As Consulta)
      _consulta = value
      TbNombre.Text = _consulta.Nombre
      TbSql.Text = _consulta.TextoSql
      CargarParametros()
    End Set
  End Property
  Private _consulta As Consulta

  Private Sub EditarConsultaUserControl_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    ComponerSqlResultado()
  End Sub

  Private Sub TbNombre_TextChanged(sender As Object, e As EventArgs) Handles TbNombre.TextChanged
    _consulta.ModificarNombre(TbNombre.Text)
  End Sub

  Private Sub TbSql_TextChanged(sender As Object, e As EventArgs) Handles TbSql.TextChanged
    If TbSql.Focused Then
      _consulta.ModificarTextoSql(TbSql.Text)
      Dim nombresParametros As IEnumerable(Of String) = Regex.Matches(_consulta.TextoSql, PatternParametro).Cast(Of Match).Select(Function(x) x.Groups(1).Value.ToUpper()).Distinct()
      Dim eliminados As IEnumerable(Of Parametro) = _consulta.ObtenerParametrosSinEliminar().Where(Function(x) Not nombresParametros.Contains(x.Nombre)).ToList()
      For Each p In eliminados
        p.Consulta.EliminarParametro(p)
      Next
      For Each nombreParametro In nombresParametros.Except(_consulta.ObtenerParametrosSinEliminar().Select(Function(x) x.Nombre))
        _consulta.AnadirParametro(nombreParametro, String.Empty)
      Next
      CargarParametros()
      ComponerSqlResultado()
    End If
  End Sub

  Private Sub BtnEliminarConsulta_Click(sender As Object, e As EventArgs) Handles BtnEliminarConsulta.Click
    _consulta.Informe.EliminarConsulta(_consulta)
    Dispose()
  End Sub

  Private Sub OnCambiarValorParametro(sender As Object, e As ValorParametroUserControl.ValorParametroEventArgs)
    Dim parametro As Parametro = _consulta.ObtenerParametrosSinEliminar().FirstOrDefault(Function(x) x.Nombre = e.Parametro)
    parametro.ModificarValor(e.Valor)
    ComponerSqlResultado()
    ResaltarParametro(e.Parametro, e.Valor, colorResaltarTexto)
  End Sub

  Private Sub OnSeleccionarParametro(sender As Object, e As ValorParametroUserControl.ValorParametroEventArgs)
    ResaltarParametro(e.Parametro, e.Valor, colorResaltarTexto)
  End Sub

  Private Sub OnDeseleccionarParametro(sender As Object, e As ValorParametroUserControl.ValorParametroEventArgs)
    LimpiarResaltado(TbSql)
    LimpiarResaltado(TbSqlResult)
  End Sub

  Private Sub CargarParametros()
    PnlParametros.Controls.Clear()
    For Each p In _consulta.ObtenerParametrosSinEliminar().OrderBy(Function(x) x.Nombre)
      Dim control As ValorParametroUserControl = New ValorParametroUserControl(p.Nombre, p.Valor)
      AddHandler control.CambiarValor, AddressOf OnCambiarValorParametro
      AddHandler control.Seleccionar, AddressOf OnSeleccionarParametro
      AddHandler control.Deseleccionar, AddressOf OnDeseleccionarParametro
      PnlParametros.Controls.Add(control)
    Next
  End Sub

  Private Sub ComponerSqlResultado()
    Dim textResult As String = _consulta.TextoSql.ToUpper()
    For Each p In _consulta.ObtenerParametrosSinEliminar()
      textResult = textResult.Replace(String.Format("#{0}#", p.Nombre), p.Valor)
    Next
    TbSqlResult.Text = String.Empty
    TbSqlResult.AppendText(textResult)
  End Sub

  Private Sub LimpiarResaltado(rtb As RichTextBox)
    rtb.Text = rtb.Text.ToString()
  End Sub

  Private Sub ResaltarParametro(nombreParametro As String, valor As String, color As Color)
    LimpiarResaltado(TbSql)
    ResaltarPalabra(TbSql, String.Format("#{0}#", nombreParametro), color)
    LimpiarResaltado(TbSqlResult)
    ResaltarPalabra(TbSqlResult, valor, color)
  End Sub

  Private Sub ResaltarPalabra(rtb As RichTextBox, palabra As String, color As Color)
    If Not String.IsNullOrEmpty(palabra) And rtb.Text.ToUpper().Contains(palabra.ToUpper()) Then
      Dim index As Integer = rtb.Text.ToUpper().IndexOf(palabra.ToUpper(), index + 1)
      While index <> -1
        rtb.Select(index, palabra.Length)
        rtb.SelectionBackColor = color
        index = rtb.Text.ToUpper().IndexOf(palabra.ToUpper(), index + 1)
      End While
      rtb.Select(0, 0)
    End If
  End Sub

End Class
