# SIP Dialog Listener

This library that provides a class to subscribe to SIP extensions and raise event notifications whenever dialog messages are received.

You can use this class to track things like extension status, incoming calls, call answer & termination etc. 
This class can act as a bridge between a SIP system and other internal services, enabling you to do things like call popups, call logging, display extension status or more without having to deal with SIP itself.

## Usage

To use this class:

- Create an instance of SIPAccount with the username, password etc. of the account you will register with your PBX with. 
- Create a PBXSettings instance with the PBX IP address & port and Authentication Realm. 
- Specify one or more extensions to monitor. The listener will subscribe to events for each of them.
- Create a SIPDialogueClient with the settings above and call Start() to start subscribing to events.

This library was developed for internal use and as such is fairly narrowly focused with regard to protocols and settings. It was designed to interface with 3CX primarily.