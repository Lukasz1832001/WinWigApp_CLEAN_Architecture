import { useEffect, useState, useCallback, useRef } from "react";
import * as SignalR from "@microsoft/signalr";

export interface Notification {
  id: string;
  strategyId: string;
  symbol: string;
  stockName: string;
  message: string;
  type: string; // "Buy" lub "Sell"
  createdAt: string;
  isRead: boolean;
}

export function useNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isConnected, setIsConnected] = useState(false);
  const connectionRef = useRef<SignalR.HubConnection | null>(null);

  // Utwórz połączenie SignalR
  const connectToHub = useCallback(async () => {
    try {
      const token = localStorage.getItem("token");
      if (!token) {
        console.warn("SignalR: Brak tokenu, pomijam połączenie");
        return;
      }

      const apiUrl = window.location.origin === 'http://localhost:5173' ? 'http://localhost:5262' : window.location.origin;
      const connection = new SignalR.HubConnectionBuilder()
        .withUrl(`${apiUrl}/hubs/notifications?access_token=${encodeURIComponent(token)}`, {
          skipNegotiation: false,
          transport: SignalR.HttpTransportType.WebSockets | SignalR.HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .withHubProtocol(new SignalR.JsonHubProtocol())
        .configureLogging(SignalR.LogLevel.Information)
        .build();

      // Odbierz powiadomienie
      connection.on("ReceiveNotification", (notification: Notification) => {
        console.log("SignalR: Otrzymano powiadomienie:", notification);
        setNotifications((prev) => [notification, ...prev]);
        setUnreadCount((prev) => prev + 1);
      });

      // Odbierz wiele powiadomień
      connection.on("ReceiveNotifications", (notificationsList: Notification[]) => {
        console.log("SignalR: Otrzymano powiadomienia:", notificationsList);
        setNotifications((prev) => [...notificationsList, ...prev]);
        const unreadNew = notificationsList.filter((n) => !n.isRead).length;
        setUnreadCount((prev) => prev + unreadNew);
      });

      connection.onreconnecting(() => {
        console.log("SignalR: Próba ponownego połączenia...");
        setIsConnected(false);
      });

      connection.onreconnected(() => {
        console.log("SignalR: Ponownie połączony");
        setIsConnected(true);
      });

      connection.onclose(() => {
        console.log("SignalR: Połączenie zamknięte");
        setIsConnected(false);
      });

      await connection.start();
      console.log("SignalR: Połączono z hubem powiadomień");
      setIsConnected(true);

      connectionRef.current = connection;
    } catch (error) {
      console.error("Błąd połączenia SignalR:", error);
      setIsConnected(false);
      // Spróbuj ponownie za 5 sekund
      setTimeout(connectToHub, 5000);
    }
  }, []);

  // Załaduj powiadomienia z API
  const loadNotifications = useCallback(async () => {
    try {
      const token = localStorage.getItem("token");
      if (!token) return;

      const apiUrl = window.location.origin === 'http://localhost:5173' ? 'http://localhost:5262' : window.location.origin;
      const response = await fetch(`${apiUrl}/api/notifications`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setNotifications(data);
        const unread = data.filter((n: Notification) => !n.isRead).length;
        setUnreadCount(unread);
      }
    } catch (error) {
      console.error("Błąd ładowania powiadomień:", error);
    }
  }, []);

  // Załaduj tylko nieprzeczytane powiadomienia
  const loadUnreadNotifications = useCallback(async () => {
    try {
      const token = localStorage.getItem("token");
      if (!token) return;

      const apiUrl = window.location.origin === 'http://localhost:5173' ? 'http://localhost:5262' : window.location.origin;
      const response = await fetch(`${apiUrl}/api/notifications/unread`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setUnreadCount(data.length);
      }
    } catch (error) {
      console.error("Błąd ładowania nieprzeczytanych powiadomień:", error);
    }
  }, []);

  // Oznacz jako przeczytane
  const markAsRead = useCallback(async (notificationId: string) => {
    try {
      const token = localStorage.getItem("token");
      if (!token) return;

      const apiUrl = window.location.origin === 'http://localhost:5173' ? 'http://localhost:5262' : window.location.origin;
      await fetch(`${apiUrl}/api/notifications/${notificationId}/read`, {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setNotifications((prev) =>
        prev.map((n) =>
          n.id === notificationId ? { ...n, isRead: true } : n
        )
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch (error) {
      console.error("Błąd oznaczania powiadomienia jako przeczytane:", error);
    }
  }, []);

  // Oznacz wszystkie jako przeczytane
  const markAllAsRead = useCallback(async () => {
    try {
      const token = localStorage.getItem("token");
      if (!token) return;

      const apiUrl = window.location.origin === 'http://localhost:5173' ? 'http://localhost:5262' : window.location.origin;
      await fetch(`${apiUrl}/api/notifications/read-all`, {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setNotifications((prev) =>
        prev.map((n) => ({ ...n, isRead: true }))
      );
      setUnreadCount(0);
    } catch (error) {
      console.error("Błąd oznaczania wszystkich powiadomień:", error);
    }
  }, []);

  // Usuń powiadomienie
  const deleteNotification = useCallback(async (notificationId: string) => {
    try {
      const token = localStorage.getItem("token");
      if (!token) return;

      const apiUrl = window.location.origin === 'http://localhost:5173' ? 'http://localhost:5262' : window.location.origin;
      await fetch(`${apiUrl}/api/notifications/${notificationId}`, {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setNotifications((prev) =>
        prev.filter((n) => n.id !== notificationId)
      );
      setUnreadCount((prev) => {
        const notification = notifications.find((n) => n.id === notificationId);
        return notification && !notification.isRead ? Math.max(0, prev - 1) : prev;
      });
    } catch (error) {
      console.error("Błąd usuwania powiadomienia:", error);
    }
  }, [notifications]);

  // Inicjalizacja - połącz hub i załaduj powiadomienia
  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) {
      loadNotifications();
      connectToHub();
    }

    return () => {
      // Cleanup: zamknij połączenie
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, []);

  return {
    notifications,
    unreadCount,
    isConnected,
    loadNotifications,
    loadUnreadNotifications,
    markAsRead,
    markAllAsRead,
    deleteNotification,
  };
}
