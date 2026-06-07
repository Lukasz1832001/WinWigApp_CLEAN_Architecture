import React, { useState, useEffect } from "react";
import { Link } from "react-router";
import { getStocks, StockResponse, getTechnicalIndicators } from "../../utils/stocksApi";
import { useNotifications, type Notification } from "../../hooks/useNotifications";
import { strategiesApi, StrategyResponse as Strategy } from "../../utils/strategiesApi";
import {
  Briefcase,
  TrendingUp,
  TrendingDown,
  AlertTriangle,
  Edit,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import { useUser } from "../../context/UserContext";

interface PortfolioPosition {
  symbol: string;
  name: string;
  quantity: number;
  avgPrice: number;
  stopLoss: number | null;
}

interface PortfolioResponse {
  items: PortfolioPosition[];
  totalValue: number;
  totalInvested: number;
  totalProfit: number;
  totalProfitPercent: number;
}

export function Portfolio() {
  const { user } = useUser();
  const { notifications } = useNotifications();
  const [portfolio, setPortfolio] = useState<PortfolioResponse>({
    items: [],
    totalValue: 0,
    totalInvested: 0,
    totalProfit: 0,
    totalProfitPercent: 0,
  });
  const [stocks, setStocks] = useState<StockResponse[]>([]);
  const [activeStrategies, setActiveStrategies] = useState<Strategy[]>([]);
  const [recommendations, setRecommendations] = useState<Record<string, any>>({});
  const [editingStopLoss, setEditingStopLoss] = useState<string | null>(null);
  const [newStopLoss, setNewStopLoss] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [updatingStopLoss, setUpdatingStopLoss] = useState<string | null>(null);

  // Mapuj notyfikacje na rekomendacje
  const getRecommendationForStock = (symbol: string) => {
    const notification = notifications.find((n) => n.symbol === symbol);
    if (!notification) return null;

    const typeMap: Record<string, string> = {
      "Buy": "KUP",
      "Sell": "SPRZEDAJ",
    };

    const colorMap: Record<string, string> = {
      "Buy": "bg-green-500/10 border-green-500/20 text-green-400",
      "Sell": "bg-red-500/10 border-red-500/20 text-red-400",
    };

    return {
      text: typeMap[notification.type] || "CZEKAJ",
      color: colorMap[notification.type] || "bg-yellow-500/10 border-yellow-500/20 text-yellow-400",
      message: notification.message,
    };
  };

  // Funkcja do generowania rekomendacji na podstawie wskaźników technicznych
  const getStockRecommendation = (
    technicals: any,
    allowSell: boolean = false,
    strategy?: any
  ) => {
    if (!technicals || !technicals.rsi || !technicals.macd || !technicals.sma50 || !technicals.sma200) {
      console.warn("Missing technicals data", { technicals });
      return {
        text: "BRAK",
        color: "bg-gray-500/10 border-gray-500/20 text-gray-400",
      };
    }

    // Pobierz ostatnie wartości wskaźników
    const lastRsi = Array.isArray(technicals.rsi) ? technicals.rsi[technicals.rsi.length - 1] : technicals.rsi;
    const lastMacdObj = Array.isArray(technicals.macd) ? technicals.macd[technicals.macd.length - 1] : technicals.macd;
    const lastSma50 = Array.isArray(technicals.sma50) ? technicals.sma50[technicals.sma50.length - 1] : technicals.sma50;
    const lastSma200 = Array.isArray(technicals.sma200) ? technicals.sma200[technicals.sma200.length - 1] : technicals.sma200;

    // MACD histogram (obsłuż zarówno snake_case jak i camelCase)
    const macdHistogram = lastMacdObj?.Histogram ?? lastMacdObj?.histogram ?? 0;

    // Jeśli nie ma strategii, użyj domyślnych parametrów
    const rsiLow = strategy?.rsiLow ?? 30;
    const rsiHigh = strategy?.rsiHigh ?? 70;
    const macdBuy = strategy?.macdBuy ?? true;
    const sma50Above200 = strategy?.sma50Above200 ?? true;

    console.log("Stock recommendation analysis:", {
      symbol: technicals.currentPrice ? "N/A" : "N/A",
      lastRsi,
      macdHistogram,
      lastSma50,
      lastSma200,
      strategy: { rsiLow, rsiHigh, macdBuy, sma50Above200 },
    });

    // Sprawdź sygnał BUY
    // Warunek 1: RSI < RsiLow (niedowartościowana)
    // Warunek 2: Jeśli strategia wymaga MACD buy -> histogram > 0
    // Warunek 3: Jeśli strategia wymaga SMA50>SMA200 -> sma50 > sma200
    const buySignal =
      lastRsi < rsiLow &&
      (!macdBuy || macdHistogram > 0) &&
      (!sma50Above200 || lastSma50 > lastSma200);

    // Sprawdź sygnał SELL (jeśli dozwolony)
    // Warunek 1: RSI > RsiHigh (wykupiona)
    // Warunek 2: Jeśli strategia wymaga MACD buy -> histogram < 0 (odwrotnie)
    // Warunek 3: Jeśli strategia wymaga SMA50>SMA200 -> sma50 < sma200 (odwrotnie)
    const sellSignal =
      allowSell &&
      lastRsi > rsiHigh &&
      (!macdBuy || macdHistogram < 0) &&
      (!sma50Above200 || lastSma50 < lastSma200);

    console.log("Signal check:", { buySignal, sellSignal });

    if (buySignal) {
      return {
        text: "KUP",
        color: "bg-green-500/10 border-green-500/20 text-green-400",
      };
    }

    if (sellSignal) {
      return {
        text: "SPRZEDAJ",
        color: "bg-red-500/10 border-red-500/20 text-red-400",
      };
    }

    return {
      text: "CZEKAJ",
      color: "bg-yellow-500/10 border-yellow-500/20 text-yellow-400",
    };
  };

  const getRecommendationColor = (rec: any) => {
    if (!rec) return "";
    return rec.color || "bg-gray-500/10 border-gray-500/20 text-gray-400";
  };

  const getRecommendationText = (rec: any) => {
    if (!rec) return "";
    return rec.text || "BRAK";
  };

  useEffect(() => {
    loadPortfolio();
    loadStocks();

    // Auto-refresh portfolio every 30 seconds
    const interval = setInterval(() => {
      loadPortfolio();
    }, 30000);

    return () => clearInterval(interval);
  }, []);

  const getAuthToken = () => {
    return localStorage.getItem("token");
  };

  const loadStocks = async () => {
    try {
      const data = await getStocks();
      setStocks(data);

      // Pobierz aktywne strategie NAJPIERW
      let strategies: Strategy[] = [];
      try {
        const allStrategies = await strategiesApi.getStrategies();
        strategies = allStrategies.filter((s) => s.isActive);
        setActiveStrategies(strategies);
        console.log("Active strategies loaded:", strategies);
      } catch (strategyErr) {
        console.warn("Nie udało się pobrać strategii:", strategyErr);
      }

      // Fetch recommendations for stocks (non-blocking)
      data.forEach((stock) => {
        (async () => {
          try {
            const technicals = await getTechnicalIndicators(stock.symbol, 90);
            console.log(`Technicals for ${stock.symbol}:`, technicals);

            // Użyj pobranej strategii, a nie state (unika race condition)
            const strategy = strategies.length > 0 ? strategies[0] : undefined;
            console.log(`Strategy for ${stock.symbol}:`, strategy);

            const recommendation = getStockRecommendation(
              {
                ...technicals,
                currentPrice: stock.currentPrice,
              },
              true, // Portfolio mode: allow SELL recommendation
              strategy ? {
                rsiLow: strategy.rsiLow,
                rsiHigh: strategy.rsiHigh,
                macdBuy: strategy.macdBuy,
                sma50Above200: strategy.sma50Above200,
              } : undefined
            );

            console.log(`Recommendation for ${stock.symbol}:`, recommendation);

            setRecommendations((prev) => ({
              ...prev,
              [stock.symbol]: recommendation,
            }));
          } catch (err) {
            console.warn(`Failed to get recommendation for ${stock.symbol}:`, err);
            setRecommendations((prev) => ({
              ...prev,
              [stock.symbol]: {
                text: "BŁĄD",
                color: "bg-red-500/10 border-red-500/20 text-red-400",
              },
            }));
          }
        })();
      });
    } catch (error) {
      console.error("Error loading stocks:", error);
      toast.error("Nie udało się pobrać danych akcji");
    }
  };

  const loadPortfolio = async () => {
    try {
      setLoading(true);
      const token = getAuthToken();

      if (!token) {
        toast.error("Nie jesteś zalogowany");
        return;
      }

      const response = await fetch("/api/portfolio", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Nie udało się pobrać portfela");
      }

      const data: PortfolioResponse = await response.json();
      setPortfolio(data);
    } catch (error) {
      console.error("Error loading portfolio:", error);
      toast.error("Błąd podczas ładowania portfela");
    } finally {
      setLoading(false);
    }
  };

  const calculatePositionValue = (position: PortfolioPosition) => {
    const stock = stocks.find((s) => s.symbol === position.symbol);
    if (!stock) return 0;
    return stock.currentPrice * position.quantity;
  };

  const calculatePositionProfit = (position: PortfolioPosition) => {
    const stock = stocks.find((s) => s.symbol === position.symbol);
    if (!stock) return { value: 0, percent: 0 };
    const currentValue = stock.currentPrice * position.quantity;
    const investedValue = position.avgPrice * position.quantity;
    const value = currentValue - investedValue;
    const percent = ((stock.currentPrice - position.avgPrice) / position.avgPrice) * 100;
    return { value, percent };
  };

  const handleUpdateStopLoss = async (symbol: string) => {
    const stopLossValue = parseFloat(newStopLoss);
    if (isNaN(stopLossValue) || stopLossValue <= 0) {
      toast.error("Podaj prawidłową wartość stop loss");
      return;
    }

    try {
      setUpdatingStopLoss(symbol);
      const token = getAuthToken();
      if (!token) {
        toast.error("Nie jesteś zalogowany");
        return;
      }

      const response = await fetch(`/api/portfolio/${symbol}/stoploss`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ stopLoss: stopLossValue })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Nie udało się zaktualizować stop loss');
      }

      toast.success("Zaktualizowano stop loss");

      // Ensure portfolio is refreshed before closing edit mode
      await loadPortfolio();

      // Notify other components (e.g., TransactionHistory) to refresh
      window.dispatchEvent(new Event('stopLossUpdated'));

      // Close edit mode only after portfolio is reloaded
      setEditingStopLoss(null);
      setNewStopLoss("");
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Błąd podczas aktualizacji stop loss';
      toast.error(errorMessage);
      console.error('Error updating stop loss:', error);
    } finally {
      setUpdatingStopLoss(null);
    }
  };

  const handleRemoveStopLoss = async (symbol: string) => {
    try {
      setUpdatingStopLoss(symbol);
      const token = getAuthToken();
      if (!token) {
        toast.error("Nie jesteś zalogowany");
        return;
      }

      const response = await fetch(`/api/portfolio/${symbol}/stoploss`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Nie udało się usunąć stop loss');
      }

      toast.success("Usunięto stop loss");

      // Ensure portfolio is refreshed before finishing
      await loadPortfolio();

      // Notify other components (e.g., TransactionHistory) to refresh
      window.dispatchEvent(new Event('stopLossUpdated'));
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Błąd podczas usuwania stop loss';
      toast.error(errorMessage);
      console.error('Error removing stop loss:', error);
    } finally {
      setUpdatingStopLoss(null);
    }
  };

  // Calculate totals based on current stock prices (real-time updates)
  let totalValue = 0;
  let totalInvested = 0;

  portfolio.items.forEach((item) => {
    const stock = stocks.find((s) => s.symbol === item.symbol);
    if (stock) {
      totalValue += stock.currentPrice * item.quantity;
    }
    totalInvested += item.avgPrice * item.quantity;
  });

  const totalProfit = totalValue - totalInvested;
  const totalProfitPercent = totalInvested > 0 ? (totalProfit / totalInvested) * 100 : 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-white">Mój Portfel</h1>
        <p className="text-gray-400 mt-1">
          Przegląd Twoich inwestycji
          {activeStrategies.length > 0 && (
            <span className="ml-2 text-emerald-400">
              • Aktywna strategia: <span className="font-semibold">{activeStrategies[0].name}</span>
            </span>
          )}
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-gray-900 rounded-lg p-6 border border-gray-800">
          <div className="text-gray-400 text-sm mb-2">Wartość portfela</div>
          <div className="text-3xl font-bold text-white">
            {totalValue.toLocaleString("pl-PL", {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}{" "}
            PLN
          </div>
        </div>

        <div className="bg-gray-900 rounded-lg p-6 border border-gray-800">
          <div className="text-gray-400 text-sm mb-2">Zainwestowano</div>
          <div className="text-3xl font-bold text-white">
            {totalInvested.toLocaleString("pl-PL", {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}{" "}
            PLN
          </div>
        </div>

        <div className="bg-gray-900 rounded-lg p-6 border border-gray-800">
          <div className="text-gray-400 text-sm mb-2">Zysk/Strata</div>
          <div
            className={`text-3xl font-bold ${
              totalProfit >= 0 ? "text-emerald-500" : "text-red-500"
            }`}
          >
            {totalProfit >= 0 ? "+" : ""}
            {totalProfit.toLocaleString("pl-PL", {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}{" "}
            PLN
          </div>
          <div
            className={`text-sm mt-1 ${
              totalProfitPercent >= 0 ? "text-emerald-500" : "text-red-500"
            }`}
          >
            {totalProfitPercent >= 0 ? "+" : ""}
            {totalProfitPercent.toFixed(2)}%
          </div>
        </div>
      </div>

      <div className="bg-gray-900 rounded-lg p-6 border border-gray-800">
        <h2 className="text-xl font-bold text-white mb-6 flex items-center gap-2">
          <Briefcase className="w-5 h-5" />
          Pozycje
        </h2>

        {portfolio.items.length === 0 ? (
          <div className="text-center py-12">
            <Briefcase className="w-16 h-16 text-gray-700 mx-auto mb-4" />
            <p className="text-gray-400 mb-4">Twój portfel jest pusty</p>
            <Link
              to="/"
              className="inline-block px-6 py-3 bg-emerald-500 hover:bg-emerald-600 text-white rounded-lg transition-colors"
            >
              Przeglądaj spółki
            </Link>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-800">
                  <th className="text-left py-3 px-4 text-gray-400">Spółka</th>
                  <th className="text-right py-3 px-4 text-gray-400">Ilość</th>
                  <th className="text-right py-3 px-4 text-gray-400">Śr. cena</th>
                  <th className="text-right py-3 px-4 text-gray-400">Akt. cena</th>
                  <th className="text-right py-3 px-4 text-gray-400">Wartość</th>
                  <th className="text-right py-3 px-4 text-gray-400">Zysk/Strata</th>
                  <th className="text-center py-3 px-4 text-gray-400">Rekomendacja</th>
                  <th className="text-right py-3 px-4 text-gray-400">Stop Loss</th>
                </tr>
              </thead>
              <tbody>
                {portfolio.items.map((position) => {
                  const stock = stocks.find((s) => s.symbol === position.symbol);
                  if (!stock) return null;
                  const profit = calculatePositionProfit(position);
                  const value = calculatePositionValue(position);

                  return (
                    <tr
                      key={position.symbol}
                      className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors"
                    >
                      <td className="py-4 px-4">
                        <Link
                          to={`/stock/${position.symbol}`}
                          className="hover:text-emerald-500 transition-colors"
                        >
                          <div className="font-medium text-white">{position.symbol}</div>
                          <div className="text-sm text-gray-400">{position.name}</div>
                        </Link>
                      </td>
                      <td className="py-4 px-4 text-right text-white">
                        {position.quantity}
                      </td>
                      <td className="py-4 px-4 text-right text-white">
                        {position.avgPrice.toFixed(2)} PLN
                      </td>
                      <td className="py-4 px-4 text-right text-white">
                        {stock.currentPrice.toFixed(2)} PLN
                      </td>
                      <td className="py-4 px-4 text-right text-white font-medium">
                        {value.toFixed(2)} PLN
                      </td>
                      <td className="py-4 px-4 text-right">
                        <div
                          className={`${
                            profit.value >= 0 ? "text-emerald-500" : "text-red-500"
                          }`}
                        >
                          <div className="flex items-center justify-end gap-1">
                            {profit.value >= 0 ? (
                              <TrendingUp className="w-4 h-4" />
                            ) : (
                              <TrendingDown className="w-4 h-4" />
                            )}
                            <span className="font-medium">
                              {profit.value >= 0 ? "+" : ""}
                              {profit.value.toFixed(2)} PLN
                            </span>
                          </div>
                          <div className="text-sm">
                            {profit.percent >= 0 ? "+" : ""}
                            {profit.percent.toFixed(2)}%
                          </div>
                        </div>
                      </td>
                      <td className="py-4 px-4 text-center">
                        {recommendations[position.symbol] ? (
                          <span className={`inline-block px-3 py-1 rounded-full text-xs font-semibold border ${getRecommendationColor(recommendations[position.symbol])}`}>
                            {getRecommendationText(recommendations[position.symbol])}
                          </span>
                        ) : (
                          <span className="text-gray-500 text-xs">Ładowanie...</span>
                        )}
                      </td>
                      <td className="py-4 px-4 text-right">
                        {editingStopLoss === position.symbol ? (
                          <div className="flex items-center gap-2 justify-end">
                            <input
                              type="number"
                              step="0.01"
                              value={newStopLoss}
                              onChange={(e) => setNewStopLoss(e.target.value)}
                              placeholder="PLN"
                              className="w-24 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-white text-sm"
                              autoFocus
                              disabled={updatingStopLoss === position.symbol}
                            />
                            <button
                              onClick={() => handleUpdateStopLoss(position.symbol)}
                              disabled={updatingStopLoss === position.symbol}
                              className="px-2 py-1 bg-emerald-500 hover:bg-emerald-600 text-white rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              {updatingStopLoss === position.symbol ? "..." : "✓"}
                            </button>
                            <button
                              onClick={() => setEditingStopLoss(null)}
                              disabled={updatingStopLoss === position.symbol}
                              className="px-2 py-1 bg-gray-700 hover:bg-gray-600 text-white rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              ✕
                            </button>
                          </div>
                        ) : position.stopLoss ? (
                          <div className="flex items-center gap-2 justify-end">
                            <AlertTriangle className="w-4 h-4 text-yellow-500" />
                            <span className="text-yellow-500 text-sm">
                              {position.stopLoss.toFixed(2)} PLN
                            </span>
                            <button
                              onClick={() => {
                                setEditingStopLoss(position.symbol);
                                setNewStopLoss(position.stopLoss!.toString());
                              }}
                              disabled={updatingStopLoss === position.symbol}
                              className="p-1 text-gray-400 hover:text-emerald-500 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              <Edit className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleRemoveStopLoss(position.symbol)}
                              disabled={updatingStopLoss === position.symbol}
                              className="p-1 text-gray-400 hover:text-red-500 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setEditingStopLoss(position.symbol)}
                            className="text-gray-500 hover:text-emerald-500 text-sm transition-colors"
                          >
                            Ustaw
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
