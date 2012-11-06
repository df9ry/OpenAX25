using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{

    /**
     * The Link Multiplexer State Machjine allows one or more data links to share the same physical (radio) channel.
     * The Link Multiplexer State machine provides the logic necessary to give each data link an opportunity to use the
     * channel, according to the rotation algorithm embedded within the link multiplexer.
     * One Link Multiplexer State Machine exists per physical channel. If a single piece of equipment has multiple
     * physical channels operating simultaneously, then an independently operating Link Multiplexer State Machine
     * exists for each chhannel.
     */
    class LinkMultiplexerStateMachine
    {
    }
}
