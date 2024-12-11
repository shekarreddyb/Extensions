Option Explicit

' User inputs
Dim GitHubToken, OrgList, StartDate, EndDate, OutputFile, EnterpriseUrl
GitHubToken = InputBox("Enter your GitHub Personal Access Token:", "GitHub Token")
OrgList = InputBox("Enter the GitHub organizations (comma-separated):", "GitHub Organizations")
EnterpriseUrl = InputBox("Enter your GitHub Enterprise API base URL (e.g., https://github.mycompany.com/api/v3):", "Enterprise API URL")
StartDate = InputBox("Enter the start date (YYYY-MM-DD):", "Start Date", "2024-01-01")
EndDate = InputBox("Enter the end date (YYYY-MM-DD):", "End Date", "2024-12-31")
OutputFile = InputBox("Enter the output Excel file path:", "Output File", "C:\GitHub_Commits.xlsx")

' Constants
Const xlCenter = -4108
Const xlEdgeLeft = 7
Const xlEdgeTop = 8

' Initialize Excel
Dim ExcelApp, Workbook, Sheet
Set ExcelApp = CreateObject("Excel.Application")
ExcelApp.Visible = True
Set Workbook = ExcelApp.Workbooks.Add
Set Sheet = Workbook.Sheets(1)

' Initialize column headers
Sheet.Cells(1, 1).Value = "Organization"
Sheet.Cells(1, 2).Value = "Repository"
Sheet.Cells(1, 3).Value = "User"
Sheet.Cells(1, 4).Value = "Month"
Sheet.Cells(1, 5).Value = "Commits"
Sheet.Rows(1).Font.Bold = True

Dim Orgs, Org, RepoData, Repo, UserCommits
Dim Http, JSON, Row, ApiUrl

Row = 2
Set Http = CreateObject("MSXML2.XMLHTTP")
Set JSON = CreateObject("Scripting.Dictionary")

' Split the organizations
Orgs = Split(OrgList, ",")

' Process each organization
For Each Org In Orgs
    Org = Trim(Org)
    ApiUrl = EnterpriseUrl & "/orgs/" & Org & "/repos"
    Call GetGitHubData(ApiUrl, GitHubToken, RepoData)

    Dim RepoName
    For Each Repo In RepoData
        RepoName = Repo("name")
        
        Dim SinceDate, UntilDate
        SinceDate = StartDate
        Do While CDate(SinceDate) <= CDate(EndDate)
            UntilDate = DateAdd("m", 1, SinceDate)
            ApiUrl = EnterpriseUrl & "/repos/" & Org & "/" & RepoName & "/commits?since=" & _
                     SinceDate & "T00:00:00Z&until=" & UntilDate & "T00:00:00Z"

            Dim Commits
            Call GetGitHubData(ApiUrl, GitHubToken, Commits)

            Dim Commit, Author, MonthYear
            For Each Commit In Commits
                If Not IsNull(Commit("author")) Then
                    Author = Commit("author")("login")
                    MonthYear = FormatDateTime(SinceDate, 2) ' Format as MM/YYYY

                    ' Populate Excel
                    Sheet.Cells(Row, 1).Value = Org
                    Sheet.Cells(Row, 2).Value = RepoName
                    Sheet.Cells(Row, 3).Value = Author
                    Sheet.Cells(Row, 4).Value = MonthYear
                    Sheet.Cells(Row, 5).Value = 1

                    Row = Row + 1
                End If
            Next

            SinceDate = UntilDate
        Loop
    Next
Next

' Save the Excel file
Workbook.SaveAs OutputFile
ExcelApp.Quit
Set ExcelApp = Nothing
Set Workbook = Nothing
Set Sheet = Nothing

MsgBox "Data extraction complete. File saved at " & OutputFile, vbInformation, "Complete"

' Function to query GitHub API
Sub GetGitHubData(Url, Token, Result)
    Http.Open "GET", Url, False
    Http.setRequestHeader "Authorization", "token " & Token
    Http.setRequestHeader "User-Agent", "VBScript-GitHub-Enterprise-API"
    Http.Send

    If Http.Status = 200 Then
        Dim Response
        Response = Http.responseText
        Dim ParsedData
        Set ParsedData = JSON.parse(Response)
        Set Result = ParsedData
    Else
        MsgBox "Error fetching data from GitHub: " & Http.Status & " - " & Http.StatusText, vbCritical, "GitHub API Error"
        WScript.Quit
    End If
End Sub