export interface ChatMessage {
    jobId: string;
    username: string;
    userId?: number;
    content: string;
    timestamp: Date;
    avatarUrl?: string;
}

export interface ChannelParticipant {
    username: string;
    userId?: number;
    avatarUrl?: string;
    isTyping: boolean;
}

export class ChannelRecord {
    public jobId: string;
    public placeId?: number;
    public createdBy: string;
    public createdAt: Date;
    private participants: Map<string, ChannelParticipant> = new Map();
    private history: ChatMessage[] = [];
    private typingUsers: Set<string> = new Set();

    constructor(jobId: string, createdBy: string, userId?: number, avatarUrl?: string, placeId?: number) {
        this.jobId = jobId;
        this.placeId = placeId;
        this.createdBy = createdBy;
        this.createdAt = new Date();
    }

    public addParticipant(username: string, userId?: number, avatarUrl?: string) {
        this.participants.set(username, {
            username,
            userId,
            avatarUrl,
            isTyping: false
        });
    }

    public removeParticipant(username: string) {
        this.participants.delete(username);
        this.typingUsers.delete(username);
    }

    public getParticipant(username: string): ChannelParticipant | undefined {
        return this.participants.get(username);
    }

    public getParticipants(): ChannelParticipant[] {
        return Array.from(this.participants.values());
    }

    public appendMessage(message: ChatMessage) {
        this.history.push(message);
        if (this.history.length > 100) {
            this.history.shift();
        }
    }

    public getHistory(): ChatMessage[] {
        return [...this.history];
    }

    public setTypingState(username: string, isTyping: boolean) {
        const participant = this.participants.get(username);
        if (participant) {
            participant.isTyping = isTyping;
            if (isTyping) {
                this.typingUsers.add(username);
            } else {
                this.typingUsers.delete(username);
            }
        }
    }

    public getTypingParticipants(): string[] {
        return Array.from(this.typingUsers);
    }
}
