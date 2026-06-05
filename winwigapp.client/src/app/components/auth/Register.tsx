import { useState } from "react";
import { useNavigate, Link } from "react-router";
import { UserPlus } from "lucide-react";
import { useUser } from "../../context/UserContext";

interface ValidationErrors {
  [key: string]: string[];
}

export function Register() {
  const navigate = useNavigate();
  const { setUser } = useUser();
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    confirmPassword: "",
  });
  const [error, setError] = useState("");
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});
  const [isLoading, setIsLoading] = useState(false);

  const validateForm = (): boolean => {
    const errors: ValidationErrors = {};

    // FirstName validation
    if (!formData.firstName.trim()) {
      errors.firstName = ["Imię jest wymagane"];
    } else if (formData.firstName.length > 100) {
      errors.firstName = ["Imię nie może być dłuższe niż 100 znaków"];
    }

    // LastName validation
    if (!formData.lastName.trim()) {
      errors.lastName = ["Nazwisko jest wymagane"];
    } else if (formData.lastName.length > 100) {
      errors.lastName = ["Nazwisko nie może być dłuższe niż 100 znaków"];
    }

    // Email validation
    if (!formData.email.trim()) {
      errors.email = ["Email jest wymagany"];
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = ["Email musi być prawidłowy"];
    }

    // Password validation
    if (!formData.password) {
      errors.password = ["Hasło jest wymagane"];
    } else if (formData.password.length < 8) {
      errors.password = ["Hasło musi mieć minimum 8 znaków"];
    }

    // Confirm password validation
    if (formData.password !== formData.confirmPassword) {
      errors.confirmPassword = ["Hasła nie są identyczne"];
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    try {
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          password: formData.password
        })
      });

      if (!response.ok) {
        const errorData = await response.json();

        // Handle validation errors from backend
        if (errorData.type === "ValidationError" && errorData.errors) {
          setValidationErrors(errorData.errors);
          return;
        }

        throw new Error(errorData.message || 'Rejestracja nie powiodła się');
      }

      const data = await response.json();

      localStorage.setItem("token", data.token);
      localStorage.setItem("user", JSON.stringify(data.user));

      // Update UserContext so Layout.tsx renders immediately
      setUser(data.user);

      navigate("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Błąd podczas rejestracji");
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value,
    });
    // Clear validation error for this field when user starts typing
    if (validationErrors[name]) {
      setValidationErrors({
        ...validationErrors,
        [name]: []
      });
    }
  };

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="bg-gray-900 rounded-lg shadow-xl p-8 border border-gray-800">
          <div className="flex items-center justify-center mb-8">
            <div className="bg-emerald-500/10 p-3 rounded-lg">
              <UserPlus className="w-8 h-8 text-emerald-500" />
            </div>
          </div>

          <h1 className="text-2xl font-bold text-center mb-2 text-white">
            Utwórz konto
          </h1>
          <p className="text-gray-400 text-center mb-8">
            Rozpocznij swoją przygodę z inwestowaniem
          </p>

          {error && (
            <div className="bg-red-500/10 border border-red-500/50 text-red-500 px-4 py-3 rounded mb-6">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium text-gray-300 mb-2">
                  Imię
                </label>
                <input
                  id="firstName"
                  name="firstName"
                  type="text"
                  value={formData.firstName}
                  onChange={handleChange}
                  className={`w-full px-4 py-3 bg-gray-800 border rounded-lg text-white placeholder-gray-500 focus:outline-none focus:ring-1 transition-colors ${
                    validationErrors.firstName
                      ? "border-red-500 focus:border-red-500 focus:ring-red-500"
                      : "border-gray-700 focus:border-emerald-500 focus:ring-emerald-500"
                  }`}
                  placeholder="Jan"
                  disabled={isLoading}
                />
                {validationErrors.firstName && (
                  <p className="text-red-500 text-sm mt-1">{validationErrors.firstName[0]}</p>
                )}
              </div>

              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-gray-300 mb-2">
                  Nazwisko
                </label>
                <input
                  id="lastName"
                  name="lastName"
                  type="text"
                  value={formData.lastName}
                  onChange={handleChange}
                  className={`w-full px-4 py-3 bg-gray-800 border rounded-lg text-white placeholder-gray-500 focus:outline-none focus:ring-1 transition-colors ${
                    validationErrors.lastName
                      ? "border-red-500 focus:border-red-500 focus:ring-red-500"
                      : "border-gray-700 focus:border-emerald-500 focus:ring-emerald-500"
                  }`}
                  placeholder="Kowalski"
                  disabled={isLoading}
                />
                {validationErrors.lastName && (
                  <p className="text-red-500 text-sm mt-1">{validationErrors.lastName[0]}</p>
                )}
              </div>
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-300 mb-2">
                Email
              </label>
              <input
                id="email"
                name="email"
                type="email"
                value={formData.email}
                onChange={handleChange}
                className={`w-full px-4 py-3 bg-gray-800 border rounded-lg text-white placeholder-gray-500 focus:outline-none focus:ring-1 transition-colors ${
                  validationErrors.email
                    ? "border-red-500 focus:border-red-500 focus:ring-red-500"
                    : "border-gray-700 focus:border-emerald-500 focus:ring-emerald-500"
                }`}
                placeholder="twoj@email.com"
                disabled={isLoading}
              />
              {validationErrors.email && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.email[0]}</p>
              )}
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-300 mb-2">
                Hasło
              </label>
              <input
                id="password"
                name="password"
                type="password"
                value={formData.password}
                onChange={handleChange}
                className={`w-full px-4 py-3 bg-gray-800 border rounded-lg text-white placeholder-gray-500 focus:outline-none focus:ring-1 transition-colors ${
                  validationErrors.password
                    ? "border-red-500 focus:border-red-500 focus:ring-red-500"
                    : "border-gray-700 focus:border-emerald-500 focus:ring-emerald-500"
                }`}
                placeholder="••••••••"
                disabled={isLoading}
              />
              {validationErrors.password && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.password[0]}</p>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-300 mb-2">
                Potwierdź hasło
              </label>
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                value={formData.confirmPassword}
                onChange={handleChange}
                className={`w-full px-4 py-3 bg-gray-800 border rounded-lg text-white placeholder-gray-500 focus:outline-none focus:ring-1 transition-colors ${
                  validationErrors.confirmPassword
                    ? "border-red-500 focus:border-red-500 focus:ring-red-500"
                    : "border-gray-700 focus:border-emerald-500 focus:ring-emerald-500"
                }`}
                placeholder="••••••••"
                disabled={isLoading}
              />
              {validationErrors.confirmPassword && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.confirmPassword[0]}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-emerald-500 hover:bg-emerald-600 disabled:bg-emerald-700 disabled:cursor-not-allowed text-white font-medium py-3 px-4 rounded-lg transition-colors"
            >
              {isLoading ? "Rejestrowanie..." : "Zarejestruj się"}
            </button>
          </form>

          <div className="mt-6 text-center">
            <p className="text-gray-400">
              Masz już konto?{" "}
              <Link to="/login" className="text-emerald-500 hover:text-emerald-400 font-medium">
                Zaloguj się
              </Link>
            </p>
          </div>
        </div>

        <p className="text-center text-gray-500 text-sm mt-8">
          WIN_WIG Trading Platform 2026
        </p>
      </div>
    </div>
  );
}

