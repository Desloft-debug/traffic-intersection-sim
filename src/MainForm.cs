using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
namespace TrafficSimulation
{
    public partial class MainForm : Form
    {
        private readonly Simulation _simulation;
        private readonly System.Windows.Forms.Timer _timer;
        private DateTime _lastTime;
        // Поля управления
        private NumericUpDown _numMinSpeed;
        private NumericUpDown _numMaxSpeed;
        private NumericUpDown _numSpawnInterval;
        private Button _btnStart;
        private Button _btnReset;
        private Button _btnSave;
        public MainForm()
        {
            // 1. Сначала инициализируем базовые компоненты формы
            InitializeManualComponents();
            _simulation = new Simulation();
            _simulation.Initialize();
            // 2. Настройка таймера
            _timer = new System.Windows.Forms.Timer { Interval = 16 };
            _timer.Tick += (s, e) => OnTick();
            _lastTime = DateTime.Now;
            // Важно: не запускаем таймер сразу, если кнопка "Старт" еще не нажата
        }
        private void InitializeManualComponents()
        {
            this.Text = "Перекрёсток — Симуляция движения";
            this.Size = new Size(950, 750);
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen; // Удобно для запуска
            // Подписка на отрисовку
            this.Paint += (s, e) => _simulation.Draw(e.Graphics);
            // Создание панели управления
            var panel = new Panel {
                Dock = DockStyle.Bottom,
                Height = 180, // Немного увеличим для отступов
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            // Инициализация элементов управления
            _numMinSpeed = new NumericUpDown { Minimum = 30, Maximum = 120, Value = 40, Size = new Size(60, 25) };
            _numMaxSpeed = new NumericUpDown { Minimum = 30, Maximum = 120, Value = 90, Size = new Size(60, 25) };
            _numSpawnInterval = new NumericUpDown { Minimum = 0.5m, Maximum = 10, Value = 2.0m, DecimalPlaces = 1, Size = new Size(70, 25) };
            _btnStart = new Button { Text = "Старт", Size = new Size(90, 30) };
            _btnReset = new Button { Text = "Сброс", Size = new Size(90, 30) };
            _btnSave = new Button { Text = "Сохранить отчёт", Size = new Size(130, 30) };
            // Разметка (Layout)
            int x = 20, y = 20;
            panel.Controls.Add(new Label { Text = "Мин. скорость (км/ч):", Location = new Point(x, y), AutoSize = true });
            _numMinSpeed.Location = new Point(x + 130, y);
            panel.Controls.Add(_numMinSpeed);
            x += 210;
            panel.Controls.Add(new Label { Text = "Макс. скорость (км/ч):", Location = new Point(x, y), AutoSize = true });
            _numMaxSpeed.Location = new Point(x + 135, y);
            panel.Controls.Add(_numMaxSpeed);
            x = 20; y += 40;
            panel.Controls.Add(new Label { Text = "Интервал появления (сек):", Location = new Point(x, y), AutoSize = true });
            _numSpawnInterval.Location = new Point(x + 160, y);
            panel.Controls.Add(_numSpawnInterval);
            x = 20; y += 40;
            _btnStart.Location = new Point(x, y);
            _btnStart.Click += (s, e) => {
                _timer.Enabled = !_timer.Enabled;
                _btnStart.Text = _timer.Enabled ? "Стоп" : "Старт";
            };
            panel.Controls.Add(_btnStart);
            _btnReset.Location = new Point(x + 100, y);
            _btnReset.Click += (s, e) => {
                _timer.Stop();
                _simulation.Initialize();
                _btnStart.Text = "Старт";
                this.Invalidate();
            };
            panel.Controls.Add(_btnReset);
            _btnSave.Location = new Point(x + 200, y);
            _btnSave.Click += OnSaveClick;
            panel.Controls.Add(_btnSave);
            // Инструкции
            var lblInfo = new Label {
                Text = "• Настройте параметры и нажмите Старт\n• Статистика сохраняется на Рабочий стол",
                Location = new Point(20, y + 40),
                Size = new Size(400, 40),
                ForeColor = Color.DarkSlateGray
            };
            panel.Controls.Add(lblInfo);
            this.Controls.Add(panel);
        }
        private void OnSaveClick(object sender, EventArgs e)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"traffic_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string path = Path.Combine(desktop, fileName);
                _simulation.Statistics.SaveToFile(path);
                MessageBox.Show($"Отчёт создан: {fileName}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void OnTick()
        {
            // Актуализация параметров из UI
            _simulation.MinSpeedKmh = (double)_numMinSpeed.Value;
            _simulation.MaxSpeedKmh = (double)_numMaxSpeed.Value;
            _simulation.CarSpawnInterval = (double)_numSpawnInterval.Value;
            double dt = (DateTime.Now - _lastTime).TotalSeconds;
            // Ограничиваем dt, чтобы при фризах машины не "телепортировались"
            if (dt > 0.1) dt = 0.1;
            _lastTime = DateTime.Now;
            _simulation.Update(dt);
            this.Invalidate(); // Перерисовка формы
        }
    }
}
