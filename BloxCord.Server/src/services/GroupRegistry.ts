import { v4 as uuidv4 } from 'uuid';

export interface GroupMessage {
    groupId: string;
    fromUserId: number;
    fromUsername: string;
    content: string;
    timestamp: Date;
}

export interface GroupChat {
    groupId: string;
    name?: string;
    participants: number[]; // UserIds
    messages: GroupMessage[];
    createdBy: number;
    createdAt: Date;
}

export class GroupRegistry {
    private groups: Map<string, GroupChat> = new Map();

    public createGroup(creatorId: number, participants: number[], name?: string): GroupChat {
        const groupId = uuidv4();
        // Ensure creator is in participants
        const allParticipants = Array.from(new Set([...participants, creatorId]));
        
        const group: GroupChat = {
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

    public getGroup(groupId: string): GroupChat | undefined {
        return this.groups.get(groupId);
    }

    public addMessage(groupId: string, fromUserId: number, fromUsername: string, content: string): GroupMessage | undefined {
        const group = this.groups.get(groupId);
        if (!group) return undefined;

        const message: GroupMessage = {
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

    public getUserGroups(userId: number): GroupChat[] {
        const result: GroupChat[] = [];
        for (const group of this.groups.values()) {
            if (group.participants.includes(userId)) {
                result.push(group);
            }
        }
        return result;
    }
}
