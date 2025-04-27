// --- Domain Classes ---

public class Transaction
{
    public Guid Id { get; private set; }
    public string Operation { get; private set; } // "Create", "Update", "Delete"
    public string Status { get; private set; } // "Pending", "InProgress", "Completed", "Failed"
    public int Progress { get; private set; } // 0-100%

    public Guid DatabaseId { get; private set; }
    public Database Database { get; private set; }

    public List<TransactionDetail> TransactionDetails { get; private set; } = new();

    // --- Aggregate Root Method ---
    public void UpdateProgressAndStatus()
    {
        if (TransactionDetails == null || !TransactionDetails.Any())
            return;

        foreach (var transactionDetail in TransactionDetails)
        {
            transactionDetail.UpdateProgressAndStatus();
        }

        Progress = (int)TransactionDetails.Average(td => td.Progress);

        if (TransactionDetails.All(td => td.Status == "Completed"))
        {
            Status = "Completed";
            HandleCompletionEffects();
        }
        else if (TransactionDetails.Any(td => td.Status == "Failed"))
        {
            Status = "Failed";
        }
        else
        {
            Status = "InProgress";
        }
    }

    private void HandleCompletionEffects()
    {
        if (Database == null)
            return;

        if (Operation == "Create")
        {
            if (Status == "Completed")
            {
                Database.Status = "Active";
                foreach (var detail in Database.DatabaseDetails)
                {
                    detail.Status = "Active";
                }
            }
            else if (Status == "Failed")
            {
                Database.Status = "CreationFailed";
                foreach (var detail in Database.DatabaseDetails)
                {
                    detail.Status = "CreationFailed";
                }
            }
        }
        else if (Operation == "Update")
        {
            if (Status == "Completed")
            {
                Database.Status = "Updated";
            }
            else if (Status == "Failed")
            {
                Database.Status = "UpdateFailed";
            }
        }
        else if (Operation == "Delete")
        {
            if (Status == "Completed")
            {
                Database.Status = "Deleted";
                foreach (var detail in Database.DatabaseDetails)
                {
                    detail.Status = "Deleted";
                }
            }
            else if (Status == "Failed")
            {
                Database.Status = "DeleteFailed";
            }
        }
    }
}

public class TransactionDetail
{
    public Guid Id { get; private set; }
    public string Status { get; private set; } // "Pending", "InProgress", "Completed", "Failed"
    public int Progress { get; private set; } // 0-100%

    public Guid DatabaseDetailId { get; private set; }
    public DatabaseDetail DatabaseDetail { get; private set; }

    public List<OperationTask> OperationTasks { get; private set; } = new();

    // --- Child Aggregate Method ---
    public void UpdateProgressAndStatus()
    {
        if (OperationTasks == null || !OperationTasks.Any())
            return;

        var completedTasks = OperationTasks.Count(t => t.Status == "Completed" || t.Status == "Skipped");
        var totalTasks = OperationTasks.Count;

        Progress = totalTasks > 0 ? (completedTasks * 100) / totalTasks : 0;

        if (OperationTasks.All(t => t.Status == "Completed" || t.Status == "Skipped"))
        {
            Status = "Completed";
        }
        else if (OperationTasks.Any(t => t.Status == "Failed"))
        {
            Status = "Failed";
        }
        else
        {
            Status = "InProgress";
        }
    }
}

public class OperationTask
{
    public Guid Id { get; private set; }
    public string Status { get; private set; } // "Pending", "InProgress", "Completed", "Failed", "Skipped"
}