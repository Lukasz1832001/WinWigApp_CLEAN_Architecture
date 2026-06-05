import React, { useState, useEffect } from "react";
import { Link } from "react-router";
import { getStocks, StockResponse } from "../../utils/stocksApi";
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
  const [portfolio, setPortfolio] = useState<PortfolioResponse>({
    items: [],
    totalValue: 0,
    totalInvested: 0,
    totalProfit: 0,
    totalProfitPercent: 0,
  });
  const [stocks, setStocks] = useState<StockResponse[]>([]);
  const [editingStopLoss, setEditingStopLoss] = useState<string | null>(null);
  const [newStopLoss, setNewStopLoss] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [updatingStopLoss, setUpdatingStopLoss] = useState<string | null>(null);

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
        <p className="text-gray-400 mt-1">Przegląd Twoich inwestycji</p>
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
