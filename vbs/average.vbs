Sub CalculateAverageResolveTime()
    Dim ws As Worksheet, newSheet As Worksheet
    Dim lastRow As Long, resultRow As Long
    Dim priorityDict As Object, resolveTimeDict As Object
    Dim cell As Range, key As Variant
    Dim createdOn As Date, resolveTime As Variant
    Dim priority As String
    Dim currentTime As Date
    Dim avgTime As Double
    
    ' Set the active sheet where data exists
    Set ws = ActiveSheet
    
    ' Find the last row with data
    lastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row
    
    ' Initialize dictionaries
    Set priorityDict = CreateObject("Scripting.Dictionary")
    Set resolveTimeDict = CreateObject("Scripting.Dictionary")
    
    ' Get the current time
    currentTime = Now()
    
    ' Loop through data (Assuming headers are in Row 1)
    For Each cell In ws.Range("A2:A" & lastRow) ' Assuming Priority is in Column A
        priority = cell.Value
        createdOn = ws.Cells(cell.Row, 2).Value ' Assuming CreatedOn is in Column B
        resolveTime = ws.Cells(cell.Row, 3).Value ' Assuming ResolveTime is in Column C
        
        ' If ResolveTime is blank, calculate the difference between CreatedOn and current time
        If IsEmpty(resolveTime) Or resolveTime = "" Then
            resolveTime = DateDiff("s", createdOn, currentTime)
        End If
        
        ' Store values in dictionary
        If Not priorityDict.exists(priority) Then
            priorityDict.Add priority, 1
            resolveTimeDict.Add priority, resolveTime
        Else
            priorityDict(priority) = priorityDict(priority) + 1
            resolveTimeDict(priority) = resolveTimeDict(priority) + resolveTime
        End If
    Next cell
    
    ' Create a new sheet for results
    On Error Resume Next
    Set newSheet = ThisWorkbook.Sheets("Average Resolve Time")
    If newSheet Is Nothing Then
        Set newSheet = ThisWorkbook.Sheets.Add
        newSheet.Name = "Average Resolve Time"
    End If
    On Error GoTo 0
    
    ' Clear old data in the results sheet
    newSheet.Cells.Clear
    
    ' Write headers
    newSheet.Cells(1, 1).Value = "Priority"
    newSheet.Cells(1, 2).Value = "Average Resolve Time (Seconds)"
    
    ' Write results to new sheet
    resultRow = 2
    For Each key In priorityDict.keys
        avgTime = resolveTimeDict(key) / priorityDict(key)
        newSheet.Cells(resultRow, 1).Value = key
        newSheet.Cells(resultRow, 2).Value = avgTime
        resultRow = resultRow + 1
    Next key
    
    ' Autofit columns
    newSheet.Columns("A:B").AutoFit
    
    ' Notify user
    MsgBox "Average resolve time calculated and saved in 'Average Resolve Time' sheet.", vbInformation, "Process Completed"
    
    ' Cleanup
    Set priorityDict = Nothing
    Set resolveTimeDict = Nothing
End Sub