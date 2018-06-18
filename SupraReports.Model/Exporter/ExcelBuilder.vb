﻿Imports System.IO
Imports OfficeOpenXml

Public Class ExcelBuilder
  Implements IDisposable

  Public Sub New(fileNamePrefix As String)
    Dim fileName As String = String.Format("{0}_{1}.xlsx", fileNamePrefix, Now.ToString("yyyyMMdd_HHmmss"))
    Dim outputFile As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName)
    excel = New ExcelPackage(New FileInfo(outputFile))
  End Sub

  Private excel As ExcelPackage

  Public Function AddWorksheet(worksheetName As String, contents As IDataReader) As ExcelBuilder
    If String.IsNullOrEmpty(worksheetName) Then worksheetName = String.Format("sheet_{0}", excel.Workbook.Worksheets.Count)
    Dim worksheet = excel.Workbook.Worksheets.Add(worksheetName)
    If contents IsNot Nothing Then
      worksheet.Cells("A1").LoadFromDataReader(contents, True, worksheetName, Table.TableStyles.Medium10)
    End If
    Return Me
  End Function

  Public Sub Build()
    excel.Save()
  End Sub

#Region "IDisposable"
  Private disposedValue As Boolean

  Protected Overridable Sub Dispose(disposing As Boolean)
    If Not disposedValue Then
      If disposing Then
        excel.Dispose()
      End If
    End If
    disposedValue = True
  End Sub

  Public Sub Dispose() Implements IDisposable.Dispose
    Dispose(True)
  End Sub
#End Region
End Class