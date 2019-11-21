# ChatBubble v0.3 # 

## Social Media App ##

This is a social media app that I've been working on for the past few months. It consists of 2 executable pieces of software,
the server console and the client, as well as a few custom-made libraries. UI is based on WinForms (sadly).

Users can sign up, log in, edit their description, search for, add, delete friends and look up their description. Upcoming
features are outlined below.

This software works around a simplified multiple flat file implementation of a database that stores every user's profile information
and allows it to be edited, read and modified in any necessary way. While a relational database like SQL would definitely be a better alternative, I decided to get some practice and try to implement my own version of a database, at least for the time being.

User profiles, passwords and logins are currently stored as plaintext, encryption will come sooner or later.

**ChatBubble.NetComponents.dll**

  - Contains all the methods responsible for the proper client-server communication and networking.

**ChatBubble.FileIOStreamer.dll**

  - This library manages all file-related IO as well as database formatting.
  
### Upcoming Features ###

  - Non-standard profile pictures.
  - Messaging, notifications, e.g. the main functionality.
  - News, posts etc.
  - Groups, ability to look up such through the Search.
  - Pending friends, friend requests, friend request notifications.
  - UI improvements.
  - Settings, password and login changes.
  - Sign up through email.
  - **Message encryption**
  
### Roadmap ###

  - [ ] Complete main functionality
  - [ ] **Migrate client to WPF, port client to mobile through Xamarin, port server console to Linux through Mono**
  - [ ] Improve UI performance through WPF.
  - [ ] Recreate database using SQL.
  - [ ] Optimize search.
  - [ ] Organize a separate server hosting network, host 24/7.
  - [ ] Open app to public testing.  
  - [ ] Implement bug reports.
  - [ ] Custom client styling, dark mode.
  - [ ] Video, photo sharing, calling
