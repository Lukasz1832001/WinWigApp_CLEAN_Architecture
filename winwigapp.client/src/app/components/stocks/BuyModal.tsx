import { useState, useEffect } from "react";
import { X, TrendingUp, TrendingDown } from "lucide-react";
import { StockResponse } from "../../utils/stocksApi";
import { toast } from "sonner";
import { useUser } from "../../context/UserContext";

interface BuyModalProps {
  stock: StockResponse;
  onClose: () => void;
}

export function BuyModal({ stock, onClose }: BuyModalProps) {
  const { user, updateBalance } = useUser();
  const [transactionType, setTransactionType] = useState<"buy" | "sell">("buy");
  const [quantity, setQuantity] = useState<string>("1");
  const [stopLoss, setStopLoss] = useState<string>("");
  const [useStopLoss, setUseStopLoss] = useState(false);
  const [availableQuantity, setAvailableQuantity] = useState(0);
  const [loadingPortfolio, setLoadingPortfolio] = useState(true);

  useEffect(() => {
    loadPortfolioData();
  }, [stock.symbol]);

  const loadPortfolioData = async () => {
    try {
      const token = localStorage.getItem("token");
      if (!token) {
        setAvailableQuantity(0);
        setLoadingPortfolio(false);
        return;
      }

      const response = await fetch("/api/portfolio", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        setAvailableQuantity(0);
      } else {
        const data = await response.json();
        const position = data.items?.find(
          (item: any) => item.symbol.toUpperCase() === stock.symbol.toUpperCase()
        );
        setAvailableQuantity(position?.quantity || 0);
      }
    } catch (error) {
      console.error("Error loading portfolio:", error);
      setAvailableQuantity(0);
    } finally {
      setLoadingPortfolio(false);
    }
  };

  const totalValue = parseFloat(quantity || "0") * stock.currentPrice;
  const balance = user?.balance || 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const qty = parseFloat(quantity);
    if (qty <= 0 || isNaN(qty)) {
      toast.error("Podaj prawidłową ilość akcji");
      return;
    }

    if (transactionType === "buy" && totalValue > balance) {
      toast.error("Niewystarczające środki na koncie");
      return;
    }

    if (transactionType === "sell" && availableQuantity === 0) {
      toast.error("Nie posiadasz tej akcji w portfelu");
      return;
    }

    if (transactionType === "sell" && qty > availableQuantity) {
      toast.error(`Posiadasz tylko ${availableQuantity} akcji tej spółki, a chcesz sprzedać ${qty}`);
      return;
    }

    if (useStopLoss) {
      const stopLossValue = parseFloat(stopLoss);
      if (isNaN(stopLossValue) || stopLossValue <= 0) {
        toast.error("Podaj prawidłową wartość stop loss");
        return;
      }
      if (transactionType === "buy" && stopLossValue >= stock.currentPrice) {
        toast.error("Stop loss musi być niższy niż aktualna cena");
        return;
      }
    }

    try {
      const token = localStorage.getItem("token");
      if (!token) {
        toast.error("Nie jesteś zalogowany");
        return;
      }

      const response = await fetch('/api/transactions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          symbol: stock.symbol,
          quantity: qty,
          price: stock.currentPrice,
          type: transactionType,
          stopLoss: useStopLoss ? parseFloat(stopLoss) : null
        })
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Transakcja nie powiodła się');
      }

      const data = await response.json();

      // Update balance in context
      updateBalance(data.newBalance);

      toast.success(
        `${transactionType === "buy" ? "Kupiono" : "Sprzedano"} ${qty} akcji ${stock.symbol}`
      );
      onClose();
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Błąd podczas transakcji';
      toast.error(errorMessage);
      console.error('Transaction error:', error);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-gray-900 rounded-lg max-w-md w-full border border-gray-800">
        <div className="flex items-center justify-between p-6 border-b border-gray-800">
          <h2 className="text-xl font-bold text-white">
            {transactionType === "buy" ? "Kup" : "Sprzedaj"} akcje
          </h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-white transition-colors"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {balance === 0 && transactionType === "buy" && (
          <div className="bg-yellow-500/10 border-t border-yellow-500/50 px-6 py-4">
            <div className="text-sm text-yellow-500">
              ⚠️ Aby kupować akcje, musisz najpierw wpłacić pieniądze na swoje konto.
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          <div className="bg-gray-800 rounded-lg p-4">
            <div className="text-sm text-gray-400 mb-1">Spółka</div>
            <div className="text-lg font-bold text-white">{stock.symbol}</div>
            <div className="text-sm text-gray-400">{stock.name}</div>
            <div className="mt-3 flex items-center justify-between">
              <span className="text-sm text-gray-400">Aktualna cena</span>
              <span className="text-lg font-bold text-white">
                {stock.currentPrice.toFixed(2)} PLN
              </span>
            </div>
          </div>

          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => setTransactionType("buy")}
              disabled={balance === 0}
              className={`flex-1 flex items-center justify-center gap-2 py-3 px-4 rounded-lg font-medium transition-colors ${
                balance === 0
                  ? "bg-gray-600 text-gray-400 cursor-not-allowed opacity-50"
                  : transactionType === "buy"
                  ? "bg-emerald-500 text-white"
                  : "bg-gray-800 text-gray-400 hover:text-white"
              }`}
              title={balance === 0 ? "Wpłać pieniądze aby kupować" : ""}
            >
              <TrendingUp className="w-5 h-5" />
              Kup
            </button>
            <button
              type="button"
              onClick={() => setTransactionType("sell")}
              disabled={loadingPortfolio || availableQuantity === 0}
              className={`flex-1 flex items-center justify-center gap-2 py-3 px-4 rounded-lg font-medium transition-colors ${
                availableQuantity === 0
                  ? "bg-gray-600 text-gray-400 cursor-not-allowed opacity-50"
                  : transactionType === "sell"
                  ? "bg-red-500 text-white"
                  : "bg-gray-800 text-gray-400 hover:text-white"
              }`}
              title={availableQuantity === 0 ? "Nie posiadasz tej akcji w portfelu" : ""}
            >
              <TrendingDown className="w-5 h-5" />
              Sprzedaj
            </button>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">
              Ilość akcji
              {transactionType === "sell" && availableQuantity > 0 && (
                <span className="text-gray-400 text-xs ml-2">
                  (Dostępnych: {availableQuantity})
                </span>
              )}
            </label>
            <input
              type="number"
              min="1"
              step="1"
              max={transactionType === "sell" ? availableQuantity : undefined}
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              className="w-full px-4 py-3 bg-gray-800 border border-gray-700 rounded-lg text-white focus:outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
              required
            />
          </div>

          <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
            <div className="flex items-center justify-between mb-2">
              <label className="flex items-center gap-2 text-sm font-medium text-gray-300 cursor-pointer">
                <input
                  type="checkbox"
                  checked={useStopLoss}
                  onChange={(e) => setUseStopLoss(e.target.checked)}
                  className="w-4 h-4 rounded bg-gray-700 border-gray-600 text-emerald-500 focus:ring-emerald-500"
                />
                Ustaw Stop Loss
              </label>
            </div>
            {useStopLoss && (
              <input
                type="number"
                step="0.01"
                value={stopLoss}
                onChange={(e) => setStopLoss(e.target.value)}
                placeholder="Cena Stop Loss (PLN)"
                className="w-full px-3 py-2 bg-gray-900 border border-gray-700 rounded text-white focus:outline-none focus:border-emerald-500"
              />
            )}
          </div>

          <div className="bg-gray-800 rounded-lg p-4 space-y-2">
            <div className="flex items-center justify-between text-sm">
              <span className="text-gray-400">{transactionType === "buy" ? "Wartość transakcji" : "Przychód ze sprzedaży"}</span>
              <span className="text-white font-medium">
                {totalValue.toFixed(2)} PLN
              </span>
            </div>
            {transactionType === "buy" ? (
              <>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-gray-400">Dostępne środki</span>
                  <span className="text-white font-medium">
                    {balance.toFixed(2)} PLN
                  </span>
                </div>
                <div className="flex items-center justify-between text-sm pt-2 border-t border-gray-700">
                  <span className="text-gray-400">Saldo po transakcji</span>
                  <span
                    className={`font-medium ${
                      balance - totalValue < 0 ? "text-red-500" : "text-emerald-500"
                    }`}
                  >
                    {(balance - totalValue).toFixed(2)} PLN
                  </span>
                </div>
              </>
            ) : (
              <>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-gray-400">Dostępnych akcji</span>
                  <span className="text-white font-medium">
                    {availableQuantity}
                  </span>
                </div>
                <div className="flex items-center justify-between text-sm pt-2 border-t border-gray-700">
                  <span className="text-gray-400">Saldo po transakcji</span>
                  <span className="text-emerald-500 font-medium">
                    {(balance + totalValue).toFixed(2)} PLN
                  </span>
                </div>
              </>
            )}
          </div>

          <button
            type="submit"
            disabled={
              (transactionType === "buy" && balance <= 0) ||
              (transactionType === "sell" && (availableQuantity === 0 || loadingPortfolio))
            }
            className={`w-full py-3 px-4 rounded-lg font-medium transition-colors ${
              transactionType === "buy"
                ? balance > 0
                  ? "bg-emerald-500 hover:bg-emerald-600 text-white"
                  : "bg-gray-600 text-gray-400 cursor-not-allowed opacity-50"
                : availableQuantity > 0
                ? "bg-red-500 hover:bg-red-600 text-white"
                : "bg-gray-600 text-gray-400 cursor-not-allowed opacity-50"
            }`}
            title={
              transactionType === "buy"
                ? balance <= 0
                  ? "Musisz wpłacić pieniądze aby kupować akcje"
                  : ""
                : availableQuantity === 0
                ? "Nie posiadasz tej akcji w portfelu"
                : ""
            }
          >
            Potwierdź {transactionType === "buy" ? "zakup" : "sprzedaż"}
          </button>
        </form>
      </div>
    </div>
  );
}
