using System;
using System.Windows;
using System.Windows.Controls;

namespace SmoAnalyzer
{
    public partial class MainWindow : Window
    {
        // Конструктор главного окна программы
        public MainWindow()
        {
            InitializeComponent();     // Инициализация компонентов интерфейса
            UpdateFieldVisibility();   // Настройка активности полей ввода
        }

        // Событие: смена типа СМО в выпадающем списке
        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtN == null) return; // Защита от вызова до инициализации окна
            UpdateFieldVisibility();  // Обновляем активные поля
            ClearResults();           // Очищаем старые результаты, чтобы не путаться
        }

        // Управление активностью (включением/выключением) полей ввода
        private void UpdateFieldVisibility()
        {
            int idx = cmbType.SelectedIndex; // Индекс выбранного типа (0-5)

            // Поле "n" (число каналов) нужно только для многоканальных СМО (индексы 3, 4, 5)
            txtN.IsEnabled = idx >= 3;

            // Поле "m" (длина очереди) нужно только для систем с ограниченной очередью (индексы 1 и 5)
            txtM.IsEnabled = (idx == 1) || (idx == 5);
        }

        // Событие: смена способа задания обслуживания (через Мю или t_об)
        private void CmbServiceMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lblService == null) return;

            // Если выбран второй пункт (индекс 1), значит задаем время (t_об)
            bool useTime = cmbServiceMode.SelectedIndex == 1;

            // Меняем подпись поля ввода
            lblService.Text = useTime
                ? "t_об — время обслуживания:"
                : "μ — интенсивность обслуживания (1/ед. вр.):";

            // Включаем/выключаем выбор единиц измерения времени
            cmbTimeUnit.IsEnabled = useTime;
            lblTimeUnit.IsEnabled = useTime;

            ClearResults(); // Очищаем результаты
        }

        // Метод для очистки поля результатов при смене настроек
        private void ClearResults()
        {
            txtResults.Text = "Настройки изменены. Нажмите «Рассчитать», чтобы получить новые результаты.";
        }

        // Получение интенсивности обслуживания (Мю) с учетом того, как её ввел пользователь
        private double GetMu()
        {
            double v = ParsePositive(txtService.Text, "значение обслуживания");

            // Если пользователь сразу ввел Мю, возвращаем как есть
            if (cmbServiceMode.SelectedIndex == 0) return v;

            // Если пользователь ввел ВРЕМЯ (t_об), переводим его в интенсивность:
            // Если единицы "как у лямбды" (например, в сутках), то Мю = 1 / t_об
            if (cmbTimeUnit.SelectedIndex == 0) return 1.0 / v;

            // Если время в минутах (а лямбда в 1/час), то переводим: Мю = 60 / t_об
            return 60.0 / v;
        }

        // Проверка: используются ли минуты (для правильного вывода времени на экран)
        private bool IsHoursMode()
            => cmbServiceMode.SelectedIndex == 1 && cmbTimeUnit.SelectedIndex == 1;

        // Парсер текста в положительное число (с заменой запятой на точку)
        private double ParsePositive(string s, string what)
        {
            if (!double.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double v))
                throw new ArgumentException($"Некорректное число в поле «{what}».");
            if (v <= 0) throw new ArgumentException($"Значение «{what}» должно быть > 0.");
            return v;
        }

        // Парсер текста в целое неотрицательное число (для n, m, k)
        private int ParseNonNegativeInt(string s, string what)
        {
            if (!int.TryParse(s, out int v) || v < 0)
                throw new ArgumentException($"Некорректное неотрицательное целое в поле «{what}».");
            return v;
        }

        // Красивое форматирование времени для вывода
        private string FmtTime(double t)
        {
            if (IsHoursMode())
                return $"{t:F4} ч ({t * 60:F2} мин)";
            return $"{t:F4} (в ед. 1/λ)";
        }

        // ГЛАВНАЯ КНОПКА: "Рассчитать характеристики"
        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Считываем данные и проверяем их на корректность
                double lambda = ParsePositive(txtLambda.Text, "λ");
                double mu = GetMu();
                int n = txtN.IsEnabled ? ParseNonNegativeInt(txtN.Text, "n") : 1;
                int m = txtM.IsEnabled ? ParseNonNegativeInt(txtM.Text, "m") : 0;

                int type = cmbType.SelectedIndex;
                string report;

                // 2. Вызываем нужную математическую функцию в зависимости от типа
                switch (type)
                {
                    case 0: report = Calc_MM1_Unlim(lambda, mu); break;
                    case 1: report = Calc_MM1_Lim(lambda, mu, m); break;
                    case 2: report = Calc_MM1_Ref(lambda, mu); break;
                    case 3: report = Calc_MMn_Ref(lambda, mu, n); break;
                    case 4: report = Calc_MMn_Unlim(lambda, mu, n); break;
                    case 5: report = Calc_MMn_Lim(lambda, mu, n, m); break;
                    default: report = "Тип СМО не выбран."; break;
                }

                // 3. Формируем справочный блок с исходными данными
                report += $"\n\n── Исходные данные ──\n";
                report += $"Тип: {((ComboBoxItem)cmbType.SelectedItem).Content}\n";
                report += $"λ = {lambda:F4}\n";

                // Показываем, как было получено Мю, чтобы не было путаницы
                if (cmbServiceMode.SelectedIndex == 0)
                    report += $"μ = {mu:F4}\n";
                else
                    report += $"t_об = {txtService.Text} (рассчитано μ = {mu:F4})\n";

                if (txtN.IsEnabled) report += $"n = {n}\n";
                if (txtM.IsEnabled) report += $"m = {m}\n";

                report += IsHoursMode()
                    ? "Единицы времени: часы/минуты (λ в 1/час, t_об в минутах)\n"
                    : "Единицы времени: все в одинаковых единицах (1/λ)\n";

                // 4. Выводим результат на экран
                txtResults.Text = report;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // КНОПКА: "P(Lq <= k)" — расчет вероятности длины очереди
        private void BtnQueueDist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double lambda = ParsePositive(txtLambda.Text, "λ");
                double mu = GetMu();
                int n = txtN.IsEnabled ? ParseNonNegativeInt(txtN.Text, "n") : 1;
                int m = txtM.IsEnabled ? ParseNonNegativeInt(txtM.Text, "m") : 0;
                int k = ParseNonNegativeInt(txtK.Text, "k");
                int type = cmbType.SelectedIndex;

                if (type >= 3 && n <= 0)
                    throw new ArgumentException("Число каналов n должно быть ≥ 1.");

                var (value, explanation) = CalcQueueDist(lambda, mu, n, m, k, type);

                MessageBox.Show(
                    $"P(Lq ≤ {k}) = {value:F6}\n\n" +
                    $"Смысл: вероятность, что в очереди находится\nне более {k} заявок.\n\n" +
                    $"Как посчитано: {explanation}",
                    "Распределение длины очереди",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Логика расчета P(Lq <= k) для кнопки
        private (double, string) CalcQueueDist(double lambda, double mu, int n, int m, int k, int type)
        {
            if (k < 0) throw new ArgumentException("k должно быть ≥ 0.");

            switch (type)
            {
                case 0: // M/M/1 неогр.
                    {
                        double rho = lambda / mu;
                        if (rho >= 1 - 1e-12) throw new InvalidOperationException($"Система нестабильна: ρ = {rho:F4} ≥ 1.");
                        double p = 1 - Math.Pow(rho, k + 2);
                        return (p, $"P(L ≤ {k + 1}) = 1 − ρ^({k + 2})");
                    }
                case 1: // M/M/1 огр.
                    {
                        if (k >= m) return (1.0, $"очередь ограничена m = {m} ⇒ P(Lq ≤ {k}) = 1");
                        double rho = lambda / mu;
                        double P0 = (Math.Abs(rho - 1) < 1e-9) ? 1.0 / (m + 2) : (1 - rho) / (1 - Math.Pow(rho, m + 2));
                        double s = 0;
                        for (int i = 0; i <= k + 1; i++) s += Math.Pow(rho, i) * P0;
                        return (s, $"P(L ≤ {k + 1}) = Σ P_i");
                    }
                case 2: // M/M/1 отказы
                case 3: // M/M/n отказы
                    return (1.0, "в системе с отказами Lq ≡ 0 ⇒ P = 1");
                case 4: // M/M/n неогр.
                    {
                        double rho0 = lambda / mu;
                        double rho = rho0 / n;
                        if (rho >= 1 - 1e-12) throw new InvalidOperationException($"Система нестабильна: ρ = {rho:F4} ≥ 1.");
                        double sum = 0, term = 1;
                        for (int i = 0; i < n; i++) { if (i > 0) term *= rho0 / i; sum += term; }
                        double lastTerm = term * rho0 / n;
                        sum += lastTerm / (1 - rho);
                        double P0 = 1.0 / sum;
                        double pSum = 0;
                        term = 1;
                        for (int i = 0; i <= n + k; i++)
                        {
                            if (i == 0) term = 1;
                            else if (i <= n) term = term * rho0 / i;
                            else term = term * rho;
                            pSum += term * P0;
                        }
                        return (pSum, $"P(L ≤ {n + k}) = Σ P_i (Эрланг C)");
                    }
                case 5: // M/M/n огр.
                    {
                        if (k >= m) return (1.0, $"очередь ограничена m = {m} ⇒ P = 1");
                        double rho0 = lambda / mu;
                        double rho = rho0 / n;
                        int cap = n + m;
                        double[] q = new double[cap + 1];
                        double term = 1, sum = 0;
                        for (int i = 0; i <= n && i <= cap; i++) { if (i > 0) term = term * rho0 / i; q[i] = term; sum += term; }
                        for (int i = n + 1; i <= cap; i++) { q[i] = q[i - 1] * rho; sum += q[i]; }
                        double P0 = 1.0 / sum;
                        double pSum = 0;
                        for (int i = 0; i <= n + k && i <= cap; i++) pSum += q[i] * P0;
                        return (pSum, $"P(L ≤ {n + k}) = Σ P_i");
                    }
                default: throw new InvalidOperationException("Тип СМО не выбран.");
            }
        }

        
        // БЛОК ОСНОВНЫХ РАСЧЁТОВ (6 ФУНКЦИЙ)
    
        // 1. M/M/1 с неограниченной очередью
        private string Calc_MM1_Unlim(double lambda, double mu)
        {
            double rho = lambda / mu;
            if (rho >= 1.0 - 1e-12) return "Система нестабильна: ρ = λ/μ ≥ 1.";

            double P0 = 1.0 - rho;                // Вероятность пустой системы
            double Pw = rho;                      // Вероятность ожидания
            double Lq = rho * rho / (1.0 - rho);  // Средняя длина очереди
            double L = rho / (1.0 - rho);         // Среднее число в системе
            double Wq = rho / (mu - lambda);      // Среднее время ожидания
            double W = 1.0 / (mu - lambda);       // Среднее время в системе

            return
                "Тип: M/M/1 с неограниченной очередью\n\n" +
                $"ρ = λ/μ                      = {rho:F4}\n" +
                $"P0 (система пуста)           = {P0:F4}\n" +
                $"P(ожидания)                  = {Pw:F4}\n" +
                $"Lq (средняя длина очереди)   = {Lq:F4}\n" +
                $"L  (среднее число в системе) = {L:F4}\n" +
                $"Wq (среднее время ожидания)  = {FmtTime(Wq)}\n" +
                $"W  (среднее время в системе) = {FmtTime(W)}";
        }

        // 2. M/M/1 с ограниченной очередью (максимум m мест)
        private string Calc_MM1_Lim(double lambda, double mu, int m)
        {
            double rho = lambda / mu;
            double P0;
            bool rhoOne = Math.Abs(rho - 1.0) < 1e-9;
            if (rhoOne) P0 = 1.0 / (m + 2);
            else P0 = (1.0 - rho) / (1.0 - Math.Pow(rho, m + 2));

            double Pref = Math.Pow(rho, m + 1) * P0;
            double L = rhoOne ? (m + 1) / 2.0
                : (rho * (1 - (m + 2) * Math.Pow(rho, m + 1) + (m + 1) * Math.Pow(rho, m + 2)))
                  / ((1 - rho) * (1 - Math.Pow(rho, m + 2)));

            double lambdaEff = lambda * (1 - Pref);
            double Lq = L - (1 - P0);
            double Wq = lambdaEff > 0 ? Lq / lambdaEff : 0;
            double W = lambdaEff > 0 ? L / lambdaEff : 0;

            return
                $"Тип: M/M/1 с ограниченной очередью (m = {m})\n\n" +
                $"ρ = λ/μ                      = {rho:F4}\n" +
                $"P0                           = {P0:F4}\n" +
                $"P(отказа)                    = {Pref:F4}\n" +
                $"Lq                           = {Lq:F4}\n" +
                $"L                            = {L:F4}\n" +
                $"Wq                           = {FmtTime(Wq)}\n" +
                $"W                            = {FmtTime(W)}";
        }

        // 3. M/M/1 с отказами
        private string Calc_MM1_Ref(double lambda, double mu)
        {
            double rho = lambda / mu;
            double P0 = 1.0 / (1.0 + rho);
            double Pref = rho / (1.0 + rho);

            return
                "Тип: M/M/1 с отказами\n\n" +
                $"ρ = λ/μ                      = {rho:F4}\n" +
                $"P0 (канал свободен)          = {P0:F4}\n" +
                $"P(отказа)                    = {Pref:F4}\n" +
                $"P(обслуживания)              = {1 - Pref:F4}";
        }

        // 4. M/M/n с отказами (Эрланг B)
        private string Calc_MMn_Ref(double lambda, double mu, int n)
        {
            if (n <= 0) return "Число каналов n должно быть ≥ 1.";
            double rho = lambda / mu;

            double sum = 0, term = 1;
            for (int k = 0; k <= n; k++)
            {
                if (k > 0) term = term * rho / k;
                sum += term;
            }
            double P0 = 1.0 / sum;
            double Pref = term * P0;
            double Lbusy = rho * (1 - Pref);

            return
                $"Тип: M/M/{n} с отказами (Эрланг B)\n\n" +
                $"ρ = λ/μ                       = {rho:F4}\n" +
                $"P0                            = {P0:F4}\n" +
                $"P(отказа)                     = {Pref:F4}\n" +
                $"P(обслуживания)               = {1 - Pref:F4}\n" +
                $"Среднее число занятых каналов = {Lbusy:F4}";
        }

        // 5. M/M/n с неограниченной очередью (Эрланг C)
        private string Calc_MMn_Unlim(double lambda, double mu, int n)
        {
            if (n <= 0) return "Число каналов n должно быть ≥ 1.";
            double rho0 = lambda / mu;
            double rho = rho0 / n;
            if (rho >= 1.0 - 1e-12) return $"Система нестабильна: ρ = {rho:F4} ≥ 1.";

            double sum = 0, term = 1;
            for (int k = 0; k < n; k++)
            {
                if (k > 0) term = term * rho0 / k;
                sum += term;
            }
            double lastTerm = term * rho0 / n;
            sum += lastTerm / (1 - rho);
            double P0 = 1.0 / sum;

            double Pq = lastTerm / (1 - rho) * P0;
            double Lq = Pq * rho / (1 - rho);
            double L = Lq + rho0;
            double Wq = Lq / lambda;
            double W = Wq + 1.0 / mu;

            return
                $"Тип: M/M/{n} с неограниченной очередью (Эрланг C)\n\n" +
                $"ρ0 = λ/μ                       = {rho0:F4}\n" +
                $"ρ  = λ/(n·μ)                   = {rho:F4}\n" +
                $"P0                             = {P0:F4}\n" +
                $"P(очереди)                     = {Pq:F4}\n" +
                $"Lq                             = {Lq:F4}\n" +
                $"L                              = {L:F4}\n" +
                $"Wq                             = {FmtTime(Wq)}\n" +
                $"W                              = {FmtTime(W)}";
        }

        // 6. M/M/n с ограниченной очередью
        private string Calc_MMn_Lim(double lambda, double mu, int n, int m)
        {
            if (n <= 0) return "Число каналов n должно быть ≥ 1.";
            if (m < 0) return "Длина очереди m должна быть ≥ 0.";

            double rho0 = lambda / mu;
            double rho = rho0 / n;
            int cap = n + m;

            double[] q = new double[cap + 1];
            double term = 1, sum = 0;
            for (int k = 0; k <= n && k <= cap; k++)
            {
                if (k > 0) term = term * rho0 / k;
                q[k] = term;
                sum += term;
            }
            for (int k = n + 1; k <= cap; k++)
            {
                q[k] = q[k - 1] * rho;
                sum += q[k];
            }
            double P0 = 1.0 / sum;
            double Pref = q[cap] * P0;

            double Lq = 0;
            for (int k = n + 1; k <= cap; k++) Lq += (k - n) * q[k] * P0;

            double lambdaEff = lambda * (1 - Pref);
            double Lbusy = rho0 * (1 - Pref);
            double L = Lq + Lbusy;
            double Wq = lambdaEff > 0 ? Lq / lambdaEff : 0;
            double W = lambdaEff > 0 ? L / lambdaEff : 0;

            return
                $"Тип: M/M/{n} с ограниченной очередью (m = {m})\n\n" +
                $"ρ0 = λ/μ                       = {rho0:F4}\n" +
                $"ρ  = λ/(n·μ)                   = {rho:F4}\n" +
                $"P0                             = {P0:F4}\n" +
                $"P(отказа)                      = {Pref:F4}\n" +
                $"Lq                             = {Lq:F4}\n" +
                $"L                              = {L:F4}\n" +
                $"Среднее число занятых каналов  = {Lbusy:F4}\n" +
                $"Wq                             = {FmtTime(Wq)}\n" +
                $"W                              = {FmtTime(W)}";
        }
    }
}