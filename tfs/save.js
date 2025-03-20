<script>
  async function saveChanges() {
    const environmentId = document.getElementById('environment').value;
    const tbody = document.getElementById('variableTableBody');
    const rows = tbody.querySelectorAll('tr');
    // Build the updated variables object; keys are variable names.
    const updatedVariables = {};
    rows.forEach(row => {
      const name = row.cells[0].querySelector('input').value;
      const value = row.cells[1].querySelector('input').value;
      const isSecret = row.cells[2].querySelector('select').value === 'true';
      updatedVariables[name] = { value, isSecret };
    });
    
    try {
      // Retrieve the complete release definition with expanded environments.
      const getUrl = `${DOMAIN}/${selectedCollection}/${selectedProject}/_apis/release/definitions/${selectedReleaseDefinition}?$expand=environments&api-version=5.1`;
      const defResponse = await fetch(getUrl);
      if (!defResponse.ok) {
        console.error('Failed to load release definition for update');
        return;
      }
      const releaseDefinition = await defResponse.json();
      
      // Update the variables for the matching environment.
      releaseDefinition.environments = releaseDefinition.environments.map(env => {
        if (env.id.toString() === environmentId.toString()) {
          env.variables = updatedVariables;
        }
        return env;
      });
      
      // Send the updated release definition.
      const updateUrl = `${DOMAIN}/${selectedCollection}/${selectedProject}/_apis/release/definitions/${selectedReleaseDefinition}?api-version=5.1`;
      const updateResponse = await fetch(updateUrl, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(releaseDefinition)
      });
      
      if (updateResponse.ok) {
        alert('Release variables updated successfully!');
        // Optionally, refresh environments for updated data.
        loadEnvironmentsForReleaseDefinition();
      } else {
        console.error('Failed to update release definition');
        alert('Error updating release variables');
      }
    } catch (error) {
      console.error('Error updating release variables:', error);
      alert('Error updating release variables');
    }
  }
</script>