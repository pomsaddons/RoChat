"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GroupRegistry = void 0;
const uuid_1 = require("uuid");
class GroupRegistry {
    constructor() {
        this.groups = new Map();
    }
    createGroup(creatorId, participants, name) {
        const groupId = (0, uuid_1.v4)();
        // Ensure creator is in participants
        const allParticipants = Array.from(new Set([...participants, creatorId]));
        const group = {
            groupId,
            name,
            participants: allParticipants,
            messages: [],
            createdBy: creatorId,
            createdAt: new Date()
        };
        this.groups.set(groupId, group);
        return group;
    }
    getGroup(groupId) {
        return this.groups.get(groupId);
    }
    addMessage(groupId, fromUserId, fromUsername, content) {
        const group = this.groups.get(groupId);
        if (!group)
            return undefined;
        const message = {
            groupId,
            fromUserId,
            fromUsername,
            content,
            timestamp: new Date()
        };
        group.messages.push(message);
        // Keep history limited?
        if (group.messages.length > 50) {
            group.messages.shift();
        }
        return message;
    }
    getUserGroups(userId) {
        const result = [];
        for (const group of this.groups.values()) {
            if (group.participants.includes(userId)) {
                result.push(group);
            }
        }
        return result;
    }
}
exports.GroupRegistry = GroupRegistry;
