# chatter
This is a Peer-to-Peer chatting example in C# using TCP/IP sockets.  Just for fun.

Need to work on the issue/bug when one program terminates, to cleanly disconnect 
the sockets so it can reconnect gracefully if the program is executed again.
Otherwise, both sides currently need to be relaunched which is not desirable.
