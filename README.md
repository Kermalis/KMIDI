# 📖 KMIDI

[![NuGet](https://img.shields.io/nuget/v/KMIDI.svg)](https://www.nuget.org/packages/KMIDI)
[![NuGet downloads](https://img.shields.io/nuget/dt/KMIDI)](https://www.nuget.org/packages/KMIDI)

This .NET library allows you to simply read and write MIDI files.
There is no functionality for playing or listening to MIDIs, so it's purely for handling the file data itself.
You can even add custom chunks to the MIDI file!
You can read MIDIs and then edit them slightly before saving them again.

It handles all MIDI formats because it tries to adhere to the MIDI specification.
The specification isn't fully respected because some invalid MIDIs are not detected at the moment.
However, the library will catch most invalid MIDIs!

----
## 🚀 Usage:
Add the [KMIDI](https://www.nuget.org/packages/KMIDI) NuGet package to your project or download the .dll from [the releases tab](https://github.com/Kermalis/KMIDI/releases).

----
## Examples:

### Reading MIDI:
```cs
MIDIFile inMIDI;
using (FileStream fs = File.OpenRead(@"C:\Folder\PathToMIDI.mid"))
{
	inMIDI = new MIDIFile(fs);
}


MIDIFormat format = inMIDI.HeaderChunk.Format;
ushort numTracks = inMIDI.HeaderChunk.NumTracks;
TimeDivisionValue timeDiv = inMIDI.HeaderChunk.TimeDivision;


foreach (MIDITrackChunk track in inMIDI.EnumerateTrackChunks())
{
	for (IMIDIEvent? e = track.First; e is not null; e = e.Next)
	{
		switch (ev)
		{
			case IMIDIEvent<NoteOnMessage> note:
			{
				NoteOnMessage msg = note.Msg;
				Console.WriteLine("Note on @{0} ticks: Channel {1}, Note {2}, Velocity {3}",
					e.Ticks, msg.Channel, msg.Note, msg.Velocity);
				break;
			}
			default:
			{
				MIDIMessage msg = e.Msg;
				Console.WriteLine("Other message @{0} ticks: {1}",
					e.Ticks, msg);
				break;
			}
		}
	}
}
```

### Writing MIDI:
```cs
ushort ticksPerQuarterNote = 48;
int tracksInitialCapacity = 2;
var newMIDI = new MIDIFile(MIDIFormat.Format1, TimeDivisionValue.CreatePPQN(ticksPerQuarterNote), tracksInitialCapacity);

var metaTrack = new MIDITrackChunk();
newMIDI.AddChunk(metaTrack);

decimal bpm = 180;
metaTrack.InsertMessage(0, MetaMessage.CreateTempoMessage(bpm));
metaTrack.InsertMessage(480, MetaMessage.CreateTextMessage(MetaMessageType.Marker, "Halfway!"));
metaTrack.InsertMessage(960, new MetaMessage(MetaMessageType.EndOfTrack, Array.Empty<byte>()));

var chanTrack = new MIDITrackChunk();
newMIDI.AddChunk(chanTrack);

byte chan = 0;
chanTrack.InsertMessage(0, MetaMessage.CreateTextMessage(MetaMessageType.TrackName, "First Track"));
chanTrack.InsertMessage(0, new ProgramChangeMessage(chan, MIDIProgram.FrenchHorn));
chanTrack.InsertMessage(0, new ControllerMessage(chan, ControllerType.Pan, 80));

chanTrack.InsertMessage(0, new NoteOnMessage(chan, MIDINote.C_4, 110));
chanTrack.InsertMessage(48, new NoteOffMessage(chan, MIDINote.C_4, 0));

chanTrack.InsertMessage(96, new NoteOnMessage(chan, MIDINote.D_4, 120));
chanTrack.InsertMessage(480, new NoteOffMessage(chan, MIDINote.D_4, 0));

chanTrack.InsertMessage(480, new NoteOnMessage(chan, MIDINote.G_4, 127));
chanTrack.InsertMessage(480, new NoteOnMessage(chan, MIDINote.C_5, 127));
chanTrack.InsertMessage(960, new NoteOffMessage(chan, MIDINote.G_4, 0));
chanTrack.InsertMessage(960, new NoteOffMessage(chan, MIDINote.C_5, 0));

chanTrack.InsertMessage(960, new MetaMessage(MetaMessageType.EndOfTrack, Array.Empty<byte>()));

using (FileStream fs = File.Create(@"C:\Folder\PathToMIDI.mid"))
{
	newMIDI.Save(fs);
}
```

----
## KMIDI Uses:
* [EndianBinaryIO](https://github.com/Kermalis/EndianBinaryIO)