import axios from 'axios';

export class RobloxAvatarService {
    private static readonly BASE_URL = 'https://thumbnails.roblox.com/v1/users/avatar-headshot';

    public async tryGetHeadshotUrl(userId: number): Promise<string | null> {
        try {
            const response = await axios.get(RobloxAvatarService.BASE_URL, {
                params: {
                    userIds: userId,
                    size: '48x48',
                    format: 'Png',
                    isCircular: true
                }
            });

            if (response.data && response.data.data && response.data.data.length > 0) {
                return response.data.data[0].imageUrl;
            }
            return null;
        } catch (error) {
            console.error('Error fetching avatar:', error);
            return null;
        }
    }
}
