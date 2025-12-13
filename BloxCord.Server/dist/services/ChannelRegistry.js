"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ChannelRegistry = void 0;
const ChannelRecord_1 = require("../models/ChannelRecord");
class ChannelRegistry {
    constructor() {
        this.channels = new Map();
    }
    createOrGetChannel(jobId, username, userId, avatarUrl, placeId) {
        let channel = this.channels.get(jobId);
        if (!channel) {
            channel = new ChannelRecord_1.ChannelRecord(jobId, username, userId, avatarUrl, placeId);
            this.channels.set(jobId, channel);
        }
        channel.addParticipant(username, userId, avatarUrl);
        return channel;
    }
    getGames() {
        const games = new Map();
        for (const channel of this.channels.values()) {
            if (!channel.placeId)
                continue;
            if (channel.getParticipants().length === 0)
                continue;
            if (channel.jobId.startsWith('-'))
                continue;
            if (!games.has(channel.placeId)) {
                games.set(channel.placeId, {
                    placeId: channel.placeId,
                    serverCount: 0,
                    playerCount: 0,
                    servers: []
                });
            }
            const game = games.get(channel.placeId);
            game.serverCount++;
            game.playerCount += channel.getParticipants().length;
            game.servers.push({
                jobId: channel.jobId,
                playerCount: channel.getParticipants().length,
                avatarUrls: channel.getParticipants()
                    .map(p => p.avatarUrl)
                    .filter(url => url)
                    .slice(0, 4)
            });
        }
        return Array.from(games.values()).sort((a, b) => b.serverCount - a.serverCount);
    }
    getChannel(jobId) {
        return this.channels.get(jobId);
    }
    searchUsers(query, jobId) {
        const lowerQuery = query.toLowerCase();
        const results = new Map();
        if (jobId) {
            const channel = this.channels.get(jobId);
            if (channel) {
                for (const p of channel.getParticipants()) {
                    if (p.username.toLowerCase().includes(lowerQuery)) {
                        results.set(p.username, p);
                    }
                }
            }
        }
        else {
            for (const channel of this.channels.values()) {
                for (const p of channel.getParticipants()) {
                    if (p.username.toLowerCase().includes(lowerQuery)) {
                        if (!results.has(p.username)) {
                            results.set(p.username, p);
                        }
                    }
                }
            }
        }
        return Array.from(results.values()).slice(0, 10);
    }
    removeParticipant(jobId, username) {
        const channel = this.channels.get(jobId);
        if (channel) {
            channel.removeParticipant(username);
            if (channel.getParticipants().length === 0) {
                this.channels.delete(jobId);
            }
        }
    }
    getParticipants(jobId) {
        const channel = this.channels.get(jobId);
        return channel ? channel.getParticipants() : [];
    }
    getTypingParticipants(jobId) {
        const channel = this.channels.get(jobId);
        return channel ? channel.getTypingParticipants() : [];
    }
    setTypingState(jobId, username, isTyping) {
        const channel = this.channels.get(jobId);
        if (channel) {
            channel.setTypingState(username, isTyping);
        }
    }
}
exports.ChannelRegistry = ChannelRegistry;
