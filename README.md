# RemoteCSharpShell

Proof-of-concept that [ScriptCs](http://scriptcs.net/) can be hosted and exposed as remote shell from .NET application via TCP.

## How to run it?
1. Clone this repository
1. Compile and run
1. Shell will be available on port 1234 
1. Use PuTTY (or other client) to connect
  * Port: 1234
  * Connection type: raw
  * Terminal->Local echo: Force off
  * Terminal->Local line editing: Force off
  * Open!
1. Play with C# shell!
1. Type 'q' to exit shell

## Nice-to-have features
* Some sort of security for connection (SSH?)
* Interacting with rest of the application (like exposing DI container used in application)
* Better line-editing 
* Persistent history
