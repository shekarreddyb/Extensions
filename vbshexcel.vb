Option Explicit

' Function to fetch data from GitHub API
Function GetGitHubData(Url As String, Token As String) As Object
    Dim Http As Object
    Dim Json As Object
    Dim Response As String
    
    Set Http = CreateObject("MSXML2.XMLHTTP")
    Http.Open "GET", Url, False
    Http.setRequestHeader "Authorization", "token " & Token
    Http.setRequestHeader "User-Agent", "Excel-GitHub-API"
    Http.Send
    
    If Http.Status = 200 Then
        Response = Http.responseText
        Set Json = JsonConverter.ParseJson(Response) ' Requires VBA JSON parser
        Set GetGitHubData = Json
    Else
        MsgBox "Error fetching data from GitHub: " & Http.Status & " - " & Http.StatusText, vbCritical
        Set GetGitHubData = Nothing
    End If
End Function

Sub FetchGitHubCommits()
    Dim GitHubToken As String, OrgList As String, EnterpriseUrl As String
    Dim StartDate As String, EndDate As String
    Dim Orgs() As String, Org As Variant
    Dim ApiUrl As String, RepoData As Object, Repo As Object
    Dim Commits As Object, Commit As Object
    Dim SinceDate As Date, UntilDate As Date
    Dim Row As Long, Sheet As Worksheet
    Dim Author As String, MonthYear As String
    
    ' Input details
    GitHubToken = InputBox("Enter your GitHub Personal Access Token:", "GitHub Token")
    OrgList = InputBox("Enter the GitHub organizations (comma-separated):", "GitHub Organizations")
    EnterpriseUrl = InputBox("Enter your GitHub Enterprise API base URL (e.g., https://github.mycompany.com/api/v3):", "Enterprise API URL")
    StartDate = InputBox("Enter the start date (YYYY-MM-DD):", "Start Date", "2024-01-01")
    EndDate = InputBox("Enter the end date (YYYY-MM-DD):", "End Date", "2024-12-31")
    
    ' Set up the sheet
    Set Sheet = ThisWorkbook.Sheets(1)
    Sheet.Cells.Clear
    Sheet.Cells(1, 1).Value = "Organization"
    Sheet.Cells(1, 2).Value = "Repository"
    Sheet.Cells(1, 3).Value = "User"
    Sheet.Cells(1, 4).Value = "Month"
    Sheet.Cells(1, 5).Value = "Commits"
    Sheet.Rows(1).Font.Bold = True
    Row = 2
    
    ' Split the organizations
    Orgs = Split(OrgList, ",")
    
    ' Process each organization
    For Each Org In Orgs
        Org = Trim(Org)
        ApiUrl = EnterpriseUrl & "/orgs/" & Org & "/repos"
        Set RepoData = GetGitHubData(ApiUrl, GitHubToken)
        
        If Not RepoData Is Nothing Then
            For Each Repo In RepoData
                Dim RepoName As String
                RepoName = Repo("name")
                
                ' Loop through months
                SinceDate = CDate(StartDate)
                Do While SinceDate <= CDate(EndDate)
                    UntilDate = DateAdd("m", 1, SinceDate)
                    ApiUrl = EnterpriseUrl & "/repos/" & Org & "/" & RepoName & "/commits?since=" & _
                             Format(SinceDate, "yyyy-mm-dd") & "T00:00:00Z&until=" & Format(UntilDate, "yyyy-mm-dd") & "T00:00:00Z"
                    
                    Set Commits = GetGitHubData(ApiUrl, GitHubToken)
                    
                    If Not Commits Is Nothing Then
                        For Each Commit In Commits
                            If Not Commit("author") Is Nothing Then
                                Author = Commit("author")("login")
                                MonthYear = Format(SinceDate, "MM/YYYY")
                                
                                ' Populate Excel
                                Sheet.Cells(Row, 1).Value = Org
                                Sheet.Cells(Row, 2).Value = RepoName
                                Sheet.Cells(Row, 3).Value = Author
                                Sheet.Cells(Row, 4).Value = MonthYear
                                Sheet.Cells(Row, 5).Value = 1
                                
                                Row = Row + 1
                            End If
                        Next Commit
                    End If
                    
                    SinceDate = UntilDate
                Loop
            Next Repo
        End If
    Next Org
    
    MsgBox "Data extraction complete.", vbInformation
End Sub