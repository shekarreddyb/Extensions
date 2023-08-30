static bool IsValidFileName(string fileName)
    {
        // Regular expression to match the given criteria.
        // The .+ at the beginning ensures that at least one character exists before 'P'.
        string pattern = @".+P\d{2}U\d{2}D\d{2}.*\.txt$";

        return Regex.IsMatch(fileName, pattern);
}
