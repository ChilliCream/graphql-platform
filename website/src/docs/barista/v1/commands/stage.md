---
title: Stage Management
---

The `barista stage` command provides a set of subcommands that allow you to manage stages.

# Edit Stages of an API 

The `barista stage edit` command  provides an interactive user interface for managing the stages of an API. The screen you see allows you to add new stages, save changes, edit existing stages, and delete stages.

```shell
barista stage edit \ 
    --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA== # optional
```

```shell
                                                                                                                        
  Edit the stages of api QX... in your/Workspace                         
                                                                                                                        
      ┌─────────┬─────────────┬─────────┐                                                      
      │ Name    │ DisplayName │ After   │                                                      
      ├─────────┼─────────────┼─────────┤                                                      
      │ dev     │ Development │         │                                                      
      │ prod    │ Production  │ dev     │                                                      
      └─────────┴─────────────┴─────────┘                                                      
                (a)dd new stage                                                                
                (s)ave changes                                                                
    press (e) to edit / press (d) to delete                                                    
```

The Console UI displays a table with the following columns:

- **Name**: The unique identifier of the stage.
- **DisplayName**: The user-friendly name that is shown in the UI.
- **After**: The stage that the current stage follows.

Below the table, you'll find several options to manage stages:

- **(a)dd new stage**: This option allows you to add a new stage to the API.
- **(s)ave changes**: This option saves any changes you've made to the stages.
- **(e)dit**: This option allows you to edit an existing stage.
- **(d)elete**: This option allows you to delete a stage.

** Hot to use the Console UI **

1. **Navigate**: Use the arrow keys on your keyboard to navigate through the table of stages.
2. **Add a new stage**: To add a new stage, press the 'a' key on your keyboard. You'll be prompted to enter the details of the new stage.
3. **Save changes**: To save any changes you've made to the stages, press the 's' key on your keyboard.
4. **Edit a stage**: To edit an existing stage, use the arrow keys to select the stage you want to edit, then press the 'e' key. You'll be prompted to update the stage's details.
5. **Delete a stage**: To delete a stage, use the arrow keys to select the stage you want to delete, then press the 'd' key. You'll be asked to confirm your decision before the stage is deleted.

Remember to save any changes you've made before exiting the Console UI.

**Options:**

- `--api-id <api-id>`: Specifies the ID of the API for which you want to edit stages. This ID can be retrieved with the `barista api list` command. You can set it from the environment variable `BARISTA_API_ID`.

# List all Stages

The `barista stage list` command is used to list all stages of an API.

```shell
barista stage list --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options:**

- `--api-id <api-id>`: Specifies the ID of the API for which you want to list the stages. This ID can be retrieved with the `barista api list` command. You can set it from the environment variable `BARISTA_API_ID`.
