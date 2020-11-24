# ChatBubble Changelog #

### 11.24.2020 - v0.4 - *"Tin Can Telephone"* ###
  - New Features:
    #### Client ####
    - Messages.
    - Message "read"/"unread" status.
    - Hardware acceleration for dialogues using DirectX.
    - New loading animation.
    - Settings tab with initial features.
    - Beginnings of DPI scaling.
    - UI optimizations.
    - Tab history.
    - Directory, server address configuration now available through config.
    - Placeholder URL resolver mechanism added.
    - Filestructure reformatting.
    
    #### Server ####
    - Multiplatform support.
    - Updated log keeping.
    - Versatile connection/error code generation.
    - Better statistics tracking.
    - Runtime endurance tests have been performed using dedicated server.
    - Some commands have been updated.
    - Some new commands have been added.
    
  - Fixed bugs
    #### Client ####
    - Client connection loss and log out issue.
    - Fixed some custom controls.
    - Dialogue fetching database bug.
    - Multiple IP per machine issue.
    - Lacking directory issues.
    - Password watermark display bugs.
    - Client window is no longer fixed to being top-most all the time.
    
    #### Server ####
    - Old code clean-up.
    - Fixed some linux filesystem detection issues.
    - Numerous small fixes.
    
  - Known bugs
    - Non-bold bugs of v0.3 may still persist.


### 11.16.2019 - v0.3 - *"Breadcrumbs"* ###

  - Changelogging started. List of main features added prior to v0.3:
    #### Client ####
    - Sign up.
    - Log in.
    - Log out *(partial)*.
    - User sessions, session time out, "keep me logged in" functionality.
    - Main profile, description editing.
    - Friend list, friend list pages, ability to add and remove friends.
    - Main search, ability to look up friends to add.
    - Ability to look up other user profiles by clicking on their profile picture.
    - UI restructuring to make it easier to include animations in the long term.
    - Numerous near-futile attempts to improve UI performance.
    
    #### Server ####
    - Server console with UI.
    - Initial, *simplified*, *multiple flat file* implementation of a database, database keeping.
    - Cookie-like authentification keeping.
    - Traffic statistics keeping (currently connected clients, logged in users).
    - Log keeping *(needs updating, currently does not work)*
    
  - Important milestones achieved:
    - Most of the framework has been laid down, it is now easier to add new features.
    - Client UI code has been fully restructured; detached from the overly simplified WinForms designer,
    making all code write-by-hand, making it easier to make UI look better in the long run.
    
  - Known bugs:
    - **On unknown occasions, client might lose connection to the server, causing it to incorrectly log out,
    causing usage problems, prompting app restart**
    - Some recently added buttons don't have any textures yet.
    - Erasing search query too fast may cause search results to fail updating.
    - Any issues in the database may cause unexpected runtime errors and logic errors to appear, since none of them
    get caught in any way yet.
