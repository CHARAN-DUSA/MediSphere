export interface Notification {
  id: number;
  userId: number;
  title: string;
  message: string;
  isRead: boolean;
  type: string;
  createdAt: string;
}

export interface BroadcastDto {
  title: string;
  message: string;
  type: string;
}
