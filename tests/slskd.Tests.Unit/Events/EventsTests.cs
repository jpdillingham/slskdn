// <copyright file="EventsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace slskd.Tests.Unit.Events;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using slskd.Events;
using Soulseek;
using Xunit;

public class EventsTests
{
    [Fact]
    public void SearchResponsesReceivedEvent_HasCorrectType()
    {
        // Arrange
        var evt = new SearchResponsesReceivedEvent
        {
            Responses = new List<SearchResponse>(),
        };

        // Assert
        Assert.Equal(EventType.SearchResponsesReceived, evt.Type);
    }

    [Fact]
    public void SearchResponsesReceivedEvent_HasUniqueId()
    {
        // Arrange
        var evt1 = new SearchResponsesReceivedEvent { Responses = new List<SearchResponse>() };
        var evt2 = new SearchResponsesReceivedEvent { Responses = new List<SearchResponse>() };

        // Assert
        Assert.NotEqual(evt1.Id, evt2.Id);
    }

    [Fact]
    public void SearchResponsesReceivedEvent_HasTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var evt = new SearchResponsesReceivedEvent { Responses = new List<SearchResponse>() };
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(evt.Timestamp >= before && evt.Timestamp <= after);
    }

    [Fact]
    public void PeerSearchedUsEvent_HasCorrectType()
    {
        // Arrange
        var evt = new PeerSearchedUsEvent
        {
            Username = "testuser",
            SearchText = "test query",
            HadResults = true,
        };

        // Assert
        Assert.Equal(EventType.PeerSearchedUs, evt.Type);
    }

    [Fact]
    public void PeerSearchedUsEvent_StoresProperties()
    {
        // Arrange & Act
        var evt = new PeerSearchedUsEvent
        {
            Username = "testuser",
            SearchText = "test query",
            HadResults = true,
        };

        // Assert
        Assert.Equal("testuser", evt.Username);
        Assert.Equal("test query", evt.SearchText);
        Assert.True(evt.HadResults);
    }

    [Fact]
    public void PeerDownloadedFromUsEvent_HasCorrectType()
    {
        // Arrange
        var evt = new PeerDownloadedFromUsEvent
        {
            Username = "testuser",
            Filename = "/music/test.flac",
        };

        // Assert
        Assert.Equal(EventType.PeerDownloadedFromUs, evt.Type);
    }

    [Fact]
    public void PeerDownloadedFromUsEvent_StoresProperties()
    {
        // Arrange & Act
        var evt = new PeerDownloadedFromUsEvent
        {
            Username = "testuser",
            Filename = "/music/test.flac",
        };

        // Assert
        Assert.Equal("testuser", evt.Username);
        Assert.Equal("/music/test.flac", evt.Filename);
    }

    [Fact]
    public void DownloadFileCompleteEvent_HasCorrectType()
    {
        // Arrange
        var evt = new DownloadFileCompleteEvent
        {
            LocalFilename = "/local/file.flac",
            RemoteFilename = "/remote/file.flac",
            Transfer = new slskd.Transfers.Transfer(),
        };

        // Assert
        Assert.Equal(EventType.DownloadFileComplete, evt.Type);
    }
}

// Note: EventBus tests require full database setup and are covered by integration tests.
// The event type tests above verify the event data structures work correctly.

