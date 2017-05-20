# chatter
This is a Peer-to-Peer chatting example in C# using TCP/IP sockets.  Just for fun.

This is far from complete.  Need to work on the issue/bug when one program terminates,
to cleanly disconnect the sockets so it can if re-ran reconnect gracefully.

## Features to add:

1 - Broadcast mode, to send to all connected peers.

2 - Better and cleaner subnet detection or automajic detection.

3 - Cleaner Thread termination so program does not hang on close.
