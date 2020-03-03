# ChatBubble Changelog #



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
