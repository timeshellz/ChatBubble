**ChatBubble v0.3** 

Social Media App

If you're reading this, good job - you've been made a collaborator. This software is closed source.

This is a social media app that I've been working on for the past few months. It consists of 2 executable pieces of software,
the server console and the client, as well as a few custom-made libraries. UI is based on WinForms (sadly).

Users can sign up, log in, edit their description, search for, add, delete friends and look up their description. Upcoming
features are outlined below.

This software works around a simplified (.txt file) implementation of a database that stores every user's profile information
and allows it to be edited, read and modified in any necessary way.

User profiles, passwords and logins are currently stored as plaintext, encryption will come sooner or later.

**ChatBubble.NetComponents.dll**

  - Contains all the methods responsible for the proper client-server communication and networking.

**ChatBubble.FileIOStreamer.dll**

  - This library manages all file-related IO as well as database formatting.
  
**Upcoming Features**

  - Non-standard profile pictures.
  - Messaging, notifications, e.g. the main functionality.
  - News, posts etc.
  - Groups, ability to look up such through the Search.
  - Pending friends, friend requests, friend request notifications.
  - UI improvements.
  - Settings, password and login changes.
  - Sign up through email.
  - **Message encryption**
  
**Roadmap**

  - Complete main functionality
  - Organize a separate server hosting network, host 24/7.
  - Open app to public testing.
  - Port app to mobile through WPF, Xamarin.
  - Improve UI performance through WPF.
  - Implement bug reports.
  - Custom client styling, dark mode.
  - Video, photo sharing, calling
  
  
