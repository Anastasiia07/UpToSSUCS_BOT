using Microsoft.Office.Interop.Excel;
using System;
using System.IO;

namespace TelegramBOT_SSU {
  class ExcelHelper : IDisposable {
    private readonly Application _excel;
    private Workbook _workbook;
    private string _filePath;

    public ExcelHelper() {
      _excel = new Application();
    }

    internal bool Open(string filePath) {
      try {
        if (File.Exists(filePath)) {
          _workbook = _excel.Workbooks.Open(filePath);
        } else {
          _workbook = _excel.Workbooks.Add();
          _filePath = filePath;
        }
        return true;
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
      return false;
    }

    internal bool Set(string column, int row, object data) {
      try {
        ((Worksheet)_excel.ActiveSheet).Cells[row, column] = data;
        return true;
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
      return false;
    }

    internal string Get(string column, int row) {
      try {
        if (((Worksheet)_excel.ActiveSheet).Cells[row, column].Value2 == null) {
          return "";
        }
        return ((Worksheet)_excel.ActiveSheet).Cells[row, column].Value2.ToString();
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
      return "";
    }

    internal void Save() {
      if (!string.IsNullOrEmpty(_filePath)) {
        _workbook.SaveAs(_filePath);
        _filePath = null;
      } else {
        _workbook.Save();
      }
    }

    public void Dispose() {
      try {
        _workbook.Close();
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
  }
}