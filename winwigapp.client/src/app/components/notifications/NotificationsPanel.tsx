import { useState, useEffect } from "react";
import {
  Bell,
  X,
  Check,
  Trash2,
  TrendingUp,
  TrendingDown,
  CheckCheck,
} from "lucide-react";
import { useNotifications, Notification } from "../../hooks/useNotifications";

export function NotificationsPanel() {
  const {
    notifications,
    unreadCount,
    isConnected,
    markAsRead,
    markAllAsRead,
    deleteNotification,
  } = useNotifications();
  const [isOpen, setIsOpen] = useState(false);

  const isBuySignal = (notification: Notification) => notification.type === "Buy";

  const getNotificationIcon = (type: string) => {
    return type === "Buy" ? (
      <TrendingUp className="w-4 h-4 text-green-500" />
    ) : (
      <TrendingDown className="w-4 h-4 text-red-500" />
    );
  };

  const getNotificationColor = (type: string) => {
    return type === "Buy"
      ? "bg-green-500/10 border-green-500/20 hover:bg-green-500/15"
      : "bg-red-500/10 border-red-500/20 hover:bg-red-500/15";
  };

  const getSignalBadgeColor = (type: string) => {
    return type === "Buy"
      ? "bg-green-500/20 text-green-200"
      : "bg-red-500/20 text-red-200";
  };

  return (
    <div className="relative">
      {/* Bell Icon Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 hover:bg-gray-900 rounded-lg transition-colors"
        title="Powiadomienia"
      >
        <Bell className="w-5 h-5 text-gray-300 hover:text-white transition-colors" />

        {/* Unread Count Badge */}
        {unreadCount > 0 && (
          <span className="absolute top-0 right-0 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center font-bold">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}

        {/* Connection Status */}
        {!isConnected && (
          <div className="absolute bottom-0 right-0 w-2 h-2 bg-yellow-500 rounded-full animate-pulse"></div>
        )}
      </button>

      {/* Dropdown Panel */}
      {isOpen && (
        <div className="absolute right-0 top-full mt-2 w-96 bg-gray-900 border border-gray-800 rounded-lg shadow-xl z-50">
          {/* Header */}
          <div className="flex items-center justify-between p-4 border-b border-gray-800">
            <div className="flex items-center gap-2">
              <Bell className="w-4 h-4 text-emerald-500" />
              <h3 className="font-semibold text-gray-100">Powiadomienia</h3>
              {unreadCount > 0 && (
                <span className="ml-2 px-2 py-1 bg-red-500/20 text-red-200 text-xs rounded-full">
                  {unreadCount} nowych
                </span>
              )}
            </div>
            <button
              onClick={() => setIsOpen(false)}
              className="text-gray-400 hover:text-gray-300"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          {/* Toolbar */}
          {notifications.length > 0 && unreadCount > 0 && (
            <div className="px-4 py-2 border-b border-gray-800 flex gap-2">
              <button
                onClick={markAllAsRead}
                className="flex items-center gap-1 px-3 py-1 text-xs bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-300 rounded-md transition-colors"
              >
                <CheckCheck className="w-3 h-3" />
                Oznacz wszystkie
              </button>
            </div>
          )}

          {/* Notifications List */}
          <div className="max-h-96 overflow-y-auto">
            {notifications.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                <Bell className="w-8 h-8 mx-auto mb-2 opacity-50" />
                <p>Brak powiadomień</p>
              </div>
            ) : (
              <div className="divide-y divide-gray-800">
                {notifications.map((notification) => (
                  <div
                    key={notification.id}
                    className={`p-4 border-l-4 ${
                      isBuySignal(notification)
                        ? "border-l-green-500"
                        : "border-l-red-500"
                    } ${getNotificationColor(
                      notification.type
                    )} transition-colors group`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1">
                        {/* Signal Badge */}
                        <div className="flex items-center gap-2 mb-2">
                          {getNotificationIcon(notification.type)}
                          <span
                            className={`text-xs font-semibold px-2 py-1 rounded ${getSignalBadgeColor(
                              notification.type
                            )}`}
                          >
                            {notification.type === "Buy" ? "KUPNO" : "SPRZEDAŻ"}
                          </span>
                          {!notification.isRead && (
                            <span className="ml-1 w-2 h-2 bg-blue-500 rounded-full"></span>
                          )}
                        </div>

                        {/* Message */}
                        <p className="text-sm text-gray-200 mb-2">
                          {notification.message}
                        </p>

                        {/* Stock Info */}
                        <div className="text-xs text-gray-400">
                          <span className="font-mono font-semibold text-gray-300">
                            {notification.symbol}
                          </span>{" "}
                          • {notification.stockName}
                        </div>

                        {/* Timestamp */}
                        <div className="text-xs text-gray-500 mt-1">
                          {new Date(notification.createdAt).toLocaleString("pl-PL", {
                            year: "numeric",
                            month: "2-digit",
                            day: "2-digit",
                            hour: "2-digit",
                            minute: "2-digit",
                          })}
                        </div>
                      </div>

                      {/* Actions */}
                      <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        {!notification.isRead && (
                          <button
                            onClick={() => markAsRead(notification.id)}
                            className="p-1 text-gray-400 hover:text-gray-200 hover:bg-gray-800 rounded transition-colors"
                            title="Oznacz jako przeczytane"
                          >
                            <Check className="w-4 h-4" />
                          </button>
                        )}
                        <button
                          onClick={() => deleteNotification(notification.id)}
                          className="p-1 text-gray-400 hover:text-red-400 hover:bg-gray-800 rounded transition-colors"
                          title="Usuń"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div className="p-3 border-t border-gray-800 text-center">
              <a
                href="/notifications"
                className="text-sm text-emerald-400 hover:text-emerald-300 transition-colors"
              >
                Wyświetl wszystkie powiadomienia →
              </a>
            </div>
          )}

          {/* Connection Status */}
          {!isConnected && (
            <div className="p-2 bg-yellow-500/10 border-t border-yellow-500/20 text-yellow-600 text-xs text-center">
              ⚠️ Brak połączenia z serwerem - powiadomienia mogą się opóźniać
            </div>
          )}
        </div>
      )}
    </div>
  );
}
