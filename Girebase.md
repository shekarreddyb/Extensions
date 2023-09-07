Rewriting history for a Git repository, especially for commits that are already pushed, can be risky. It can mess up the history for anyone who has already cloned or forked the repository. Only proceed with this if you're certain that it won't disrupt your team's workflow, or if you've communicated these changes to all stakeholders.

Here's a general guide for squashing commits:

1. **Backup the Repository**: First, make a backup of your repository just in case things go wrong.

    ```
    git clone <repository_url> <backup_folder_name>
    ```

2. **Identify the Commits**: Note the hash of the commit immediately before the first commit you want to squash.

3. **Interactive Rebase**: Use interactive rebase to squash the commits.

    ```
    git rebase -i <commit_hash_before_first_bad_commit>^
    ```

    In the text editor that opens, change the word "pick" to "squash" at the beginning of the lines for the commits you want to squash into one. Save and exit the editor.

4. **Commit Message**: Another editor will open for the combined commit message. Edit it to describe what the new, squashed commit does.

5. **Force Push**: Finally, you'll need to force-push these changes to overwrite the history on the remote repository.

    ```
    git push origin <your_branch_name> --force
    ```

**Important**: If other people have cloned the repository, they will need to take special steps to sync their history with the altered remote history.

Remember to check your repository again with TruffleHog to ensure that the sensitive data is indeed gone after these steps.
