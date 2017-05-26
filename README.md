# chatter
This is a Peer-to-Peer chatting example in C# using TCP/IP sockets.  Just for fun.

# Features:
-	Now allows drag and drop for files into the upper RichTextBox and will send a limited file size to the connected peer.
-	Much cleaner Socket connections with Acknowledgments, and can even detect if the connected users are typing.
-	Can broadcast messages to all connected user(s).
-	Debug mode allows extensive testing with console like printouts.

# Issues and Needed Improvements:
-	File links in the RickTextBox do not scroll but are fixed; another method or custom RichTextBox for custom links needs to be implemented.
-	Files dropped are sent in bulk in one message which means it limits the file size based on memory; needs to actually scream the file(s) and then save them to disk on the receiving side which memory will no longer be a concern.
-	If files are saved locally, the links in the RichTextBox can stay and not be one-shots.
