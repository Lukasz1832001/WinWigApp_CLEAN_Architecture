# Strategie Inwestycyjne - Dokumentacja

## Jak działają strategie?

### Ogólne Zasady
Strategie inwestycyjne to automatyczne reguły, które określają **kiedy kupić** i **kiedy sprzedać** akcje. Każda strategia jest zbudowana na podstawie **wskaźników technicznych**, które analizują historyczne dane cen i wolumenu.

---

## Wskaźniki Techniczne Używane w Strategiach

### 1. **RSI (Relative Strength Index)** - Siła Względna
**Co to jest?**
- Oscylator mierzący siłę i szybkość ruchów ceny
- Wartość od 0 do 100
- Wskazuje warunki **przekupienia** (overbought) i **wyprzedania** (oversold)

**Jak interpretować?**
```
RSI < 30 → Akcja WYPRZEDANA (sygnał kupna)
RSI > 70 → Akcja PRZEKUPIONA (sygnał sprzedaży)
```

**Przykład w strategii:**
- Jeśli ustawisz "RSI niski = 30" i "RSI wysoki = 70"
- System będzie szukał akcji z RSI poniżej 30 do kupna
- I będzie chciał sprzedać gdy RSI przekroczy 70

**Wytłumaczenie techniczne:**
RSI = 100 - [100 / (1 + RS)]
gdzie RS = średnia wzrostów / średnia spadków w N okresach

---

### 2. **MACD (Moving Average Convergence Divergence)** - Zbieżność/Rozbieżność
**Co to jest?**
- Wskaźnik pędu (momentum)
- Składa się z dwóch linii:
  - **MACD** (szybka średnia 12-okresowa)
  - **Linia sygnału** (powolna średnia 26-okresowa)

**Jak interpretować?**
```
MACD > Linia Sygnału → Trend WZROSTOWY (sygnał kupna)
MACD < Linia Sygnału → Trend SPADKOWY (sygnał sprzedaży)
```

**Opcja "Kupuj gdy MACD przecina linię sygnału od dołu":**
- System szuka momentu, kiedy MACD "przecina" linię sygnału **od dołu** (od góry)
- To jest **crossover** - silny sygnał kupna
- Wskazuje, że pęd zmienia się z negatywnego na pozytywny

**Wizualizacja:**
```
		 ╱─ Linia Sygnału
		╱
	╱──╱
MACD ← Przecięcie od dołu = SYGNAŁ KUPNA
```

---

### 3. **SMA (Simple Moving Average)** - Prosta Średnia Ruchoma
**Co to jest?**
- Średnia ceny z ostatnich N dni
- Wskazuje główny kierunek trendu

**W strategiach używamy dwóch:**
- **SMA 50** - średnia z 50 dni (trend krótkoterminowy)
- **SMA 200** - średnia z 200 dni (trend długoterminowy)

**Opcja "Wymagaj SMA 50 powyżej SMA 200":**
```
SMA 50 > SMA 200 → TREND WZROSTOWY (pozytywny kontekst)
SMA 50 < SMA 200 → TREND SPADKOWY (negatywny kontekst)
```

**Dlaczego to ważne?**
- Gdy SMA 50 jest powyżej SMA 200, to oznacza że ceny idą "w górę"
- Wówczas sygnały kupna są bardziej wiarygodne
- To filtruje fałszywe sygnały w trendzie spadkowym

---

## 🎯 Parametry Strategii Wyjaśnione

### 1. **Nazwa strategii**
- Opisowa nazwa, aby rozpoznać strategię
- Przykład: "Moja strategia RSI", "Strategia trendu wzrostowego"

### 2. **Planowana stopa zwrotu (%)**
- Cel zysku, jaki chcesz osiągnąć
- Przykład: `+10%` oznacza że chcesz zarobić 10% na transakcji
- System będzie monitorować, czy strategia osiąga ten cel

### 3. **Horyzont inwestycyjny (dni)**
- Jak długo chcesz trzymać akcję
- Przykład: `30 dni` = chcesz sprzedać akcję w ciągu miesiąca
- Ważne dla zarządzania ryzykiem

### 4. **RSI niski (kupno)**
- Próg, poniżej którego uważasz akcję za wyprzedaną
- Typowa wartość: `25-35`
- Niższa wartość = bardziej selektywny (czeka na "bardziej wyprzedaną" akcję)

### 5. **RSI wysoki (sprzedaż)**
- Próg, powyżej którego uważasz akcję za przekupioną
- Typowa wartość: `65-75`
- Wyższa wartość = bardziej agresywny (czeka na "bardziej przekupioną" akcję)

### 6. **MACD Kupno (checkbox)**
- Czy wymagać dodatkowo sygnału MACD?
- Jeśli zaznaczony: kupujesz TYLKO gdy MACD i RSI się zgadzają
- Jeśli nie zaznaczony: kupujesz bazując tylko na RSI

### 7. **SMA 50 > SMA 200 (checkbox)**
- Czy wymagać że krótkoterminowy trend jest wzrostowy?
- Jeśli zaznaczony: kupujesz TYLKO w trendzie wzrostowym
- Jeśli nie zaznaczony: kupujesz niezależnie od trendu

---

## 📋 Praktyczne Przykłady Strategii

### Przykład 1: "Konserwatywna - Tylko trendy wzrostowe"
```
Nazwa: "Konserwatywna strategie"
Planowana stopa zwrotu: 8%
Horyzont inwestycyjny: 45 dni
RSI niski: 35
RSI wysoki: 70
MACD Kupno: ✓ Zaznaczony
SMA 50 > SMA 200: ✓ Zaznaczony
```
**Co robi?**
- Czeka na akcje z RSI poniżej 35 (wyprzedane)
- Dodatkowo wymaga że MACD daje sygnał kupna
- WYMAGA trendu wzrostowego (SMA 50 > SMA 200)
- Sprzedaje gdy RSI > 70 lub upnie 45 dni
- ✅ **Niskie ryzyko, bardziej pewne sygnały**

---

### Przykład 2: "Agresywna - Catch falling knives"
```
Nazwa: "Agresywna"
Planowana stopa zwrotu: 15%
Horyzont inwestycyjny: 20 dni
RSI niski: 20
RSI wysoki: 75
MACD Kupno: ✗ Nie zaznaczony
SMA 50 > SMA 200: ✗ Nie zaznaczony
```
**Co robi?**
- Reaguje na BARDZO wyprzedane akcje (RSI < 20)
- Kupuje niezależnie od trendu
- Sprzedaje szybko, gdy RSI > 75
- ⚠️ **Wysokie ryzyko, ale potencjalnie szybkie zyski**

---

### Przykład 3: "Momentum - Tylko MACD"
```
Nazwa: "Momentum MACD"
Planowana stopa zwrotu: 12%
Horyzont inwestycyjny: 30 dni
RSI niski: 30
RSI wysoki: 70
MACD Kupno: ✓ Zaznaczony
SMA 50 > SMA 200: ✓ Zaznaczony
```
**Co robi?**
- Kombinacja wszystkich warunków
- Czeka na zmianę pędu (MACD)
- W trendzie wzrostowym (SMA 50 > SMA 200)
- ✅ **Zbilansowana strategia - dobre stosunek ryzyka do zysku**

---

## 🔍 Jak System Ocenia Akcje?

Gdy uruchomisz strategię (`Uruchom`), system:

1. **Pobiera dane techniczne** dla każdej akcji
   - Oblicza RSI, MACD, SMA 50, SMA 200

2. **Sprawdza warunki:**
   ```
   Czy RSI < RSI_niski?
   Czy MACD daje sygnał kupna? (jeśli wymagane)
   Czy SMA 50 > SMA 200? (jeśli wymagane)
   ```

3. **Jeśli wszystkie warunki SPEŁNIONE:**
   - ✅ Akcja otrzyma sygnał KUPNA
   - System rekomenduje kupno

4. **Po kupnie czeka na sprzedaż:**
   ```
   Czy RSI > RSI_wysoki?
   LUB
   Upłynął horyzont inwestycyjny (np. 30 dni)?
   ```
   - System sprzedaje akcję

---

## ⚙️ Implementacja w Backend-u

### Endpointy API

#### 1. **POST /api/strategies** - Utwórz strategię
```bash
curl -X POST http://localhost:5000/api/strategies \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
	"name": "Moja strategia RSI",
	"targetReturn": 10.0,
	"investmentHorizon": 30,
	"rsiLow": 30,
	"rsiHigh": 70,
	"macdBuy": true,
	"sma50Above200": false
  }'
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Moja strategia RSI",
  "targetReturn": 10.0,
  "investmentHorizon": 30,
  "rsiLow": 30,
  "rsiHigh": 70,
  "macdBuy": true,
  "sma50Above200": false,
  "isActive": false,
  "createdAt": "2026-05-04T10:30:00Z"
}
```

---

#### 2. **GET /api/strategies** - Pobierz wszystkie strategie
```bash
curl -X GET http://localhost:5000/api/strategies \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response:**
```json
[
  {
	"id": "550e8400-e29b-41d4-a716-446655440000",
	"name": "Moja strategia RSI",
	"targetReturn": 10.0,
	"investmentHorizon": 30,
	"rsiLow": 30,
	"rsiHigh": 70,
	"macdBuy": true,
	"sma50Above200": false,
	"isActive": true,
	"createdAt": "2026-05-04T10:30:00Z"
  }
]
```

---

#### 3. **GET /api/strategies/{id}** - Pobierz jedną strategię
```bash
curl -X GET http://localhost:5000/api/strategies/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

#### 4. **PUT /api/strategies/{id}** - Zaktualizuj strategię
```bash
curl -X PUT http://localhost:5000/api/strategies/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
	"name": "Moja strategia RSI - zaktualizowana",
	"targetReturn": 12.0,
	"investmentHorizon": 45,
	"rsiLow": 25,
	"rsiHigh": 75,
	"macdBuy": true,
	"sma50Above200": true
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Strategia zaktualizowana"
}
```

---

#### 5. **DELETE /api/strategies/{id}** - Usuń strategię
```bash
curl -X DELETE http://localhost:5000/api/strategies/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response:**
```json
{
  "success": true,
  "message": "Strategia usunięta"
}
```

---

#### 6. **PUT /api/strategies/{id}/toggle** - Włącz/Wyłącz strategię
```bash
curl -X PUT http://localhost:5000/api/strategies/550e8400-e29b-41d4-a716-446655440000/toggle \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response:**
```json
{
  "success": true,
  "isActive": true,
  "message": "Strategia aktywowana"
}
```

---

## 💾 Struktura Bazy Danych

```sql
CREATE TABLE Strategies (
	Id GUID PRIMARY KEY,
	UserId GUID NOT NULL,
	Name NVARCHAR(255) NOT NULL,
	TargetReturn DECIMAL(10, 2) NOT NULL,
	InvestmentHorizon INT NOT NULL,
	RsiLow DECIMAL(5, 2) NOT NULL,
	RsiHigh DECIMAL(5, 2) NOT NULL,
	MacdBuy BIT NOT NULL,
	Sma50Above200 BIT NOT NULL,
	IsActive BIT NOT NULL,
	CreatedAt DATETIME NOT NULL,
	FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

---

## 🚀 Testowanie Strategii w Postmanie

1. **Zaloguj się** - uzyskaj token JWT
2. **Utwórz strategię** - POST /api/strategies
3. **Aktywuj strategię** - PUT /api/strategies/{id}/toggle
4. **Pobierz strategie** - GET /api/strategies
5. **Edytuj strategię** - PUT /api/strategies/{id}
6. **Usuń strategię** - DELETE /api/strategies/{id}

---

## 📊 Wskazówki dla Optymalnych Strategii

| Parametr | Konserwatywna | Zbilansowana | Agresywna |
|----------|-----------|-----------|-----------|
| RSI niski | 35 | 30 | 20 |
| RSI wysoki | 70 | 70 | 75 |
| MACD | ✓ | ✓ | ✗ |
| SMA 50>200 | ✓ | ✓ | ✗ |
| Horyzont | 45 dni | 30 dni | 20 dni |
| Stopa zwrotu | 8% | 12% | 15% |
| Ryzyko | Niskie | Średnie | Wysokie |

---

## ⚠️ Ważne Ostrzeżenia

1. **Wskaźniki techniczne nie są gwarancją** - to tylko narzędzia
2. **Zawsze ustalaj stop-loss** - nie inwestuj więcej niż możesz stracić
3. **Testuj strategie na historycznych danych** - przed użyciem na rzeczywistych pieniędzach
4. **Monitoruj strategie** - rynki się zmieniają, strategie mogą stać się nieaktualne
5. **Dywersyfikacja** - nie inwestuj wszystko w jedną strategię

---

Opracowanie: WinWigApp Team
Data: 2026-05-04
