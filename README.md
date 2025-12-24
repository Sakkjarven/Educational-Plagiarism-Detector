﻿# Educational Plagiarism Detector
! ONLY IN ENGLISH YET !
## 📖 Описание
Система для автоматического обнаружения плагиата в студенческих работах с использованием методов NLP и сравнения текстов.

## 🎯 Функционал
- Анализ текстовых документов (.txt, .pdf)
- Множественные алгоритмы сравнения (Cosine Similarity, LCS, N-Gram)
- Визуализация результатов
- Автоматические отчеты через CI/CD

## 🚀 Быстрый старт

### Требования
- .NET 8.0 SDK или выше

### Установка и запуск
```bash
# Клонирование репозитория
git clone https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git
cd YOUR_REPO_NAME

# Восстановление зависимостей
dotnet restore

# Запуск анализа документов
dotnet run --project src/PlagiarismChecker.Cli -- -i sample-data
# Базовый анализ
plagiarism-checker -i ./documents

# С настройками
plagiarism-checker \
  -i ./student-submissions \
  -a CosineSimilarity,NGram \
  -t 0.3 \
  -o results.json \
  --no-matrix

# Показать справку
plagiarism-checker --help
```


## 📊 Алгоритмы
Cosine Similarity - Косинусное сходство с TF-IDF векторизацией

Longest Common Subsequence (LCS) - Наибольшая общая подпоследовательность

N-Gram Matching - Сравнение N-грамм с коэффициентом Жаккара

## 🛠️ Технологии
- .NET 8
- C#
- GitHub Actions
- Spectre.Console (для визуализации)

## 📁 Структура проекта\
EducationalPlagiarismDetector/\
├── src/\
│   ├── PlagiarismChecker.Core/     # Основная логика\
│   ├── PlagiarismChecker.Cli/      # Консольное приложение\
│   └── PlagiarismChecker.Tests/    # Юнит-тесты\
├── .github/workflows/              # CI/CD конфигурации\
│   ├── dotnet-build-test.yml       # Основной workflow\
│   └── scheduled-analysis.yml      # Креативный workflow\
├── sample-data/                    # Примеры документов\
├── scripts/                        # Вспомогательные скрипты\
└── docs/                           # Документация\
## 🔧 CI/CD Pipeline
Проект использует два workflow:

1. Основной (Build & Test)
Автоматический запуск на push/pull request

Сборка проекта

Запуск всех юнит-тестов

2. Креативный (Scheduled Analysis)
Ежедневный запуск в 08:00 UTC

Ручной запуск с параметрами через workflow_dispatch

Анализ примеров документов

Сохранение результатов как артефактов

## 🤝 Вклад в проект
Форкни репозиторий

Создай ветку для фичи (git checkout -b feature/amazing-feature)

Закоммить изменения (git commit -m 'Add amazing feature')

Запушь в ветку (git push origin feature/amazing-feature)

Открой Pull Request

## 📞 Поддержка
Для вопросов и предложений создавайте Issue в репозитории.

### **3. Добавляем пример бонусного функционала (опционально)**

**Файл: `scripts/generate-sample-data.ps1`** (для Windows):
```powershell
# Скрипт для генерации тестовых данных
$sampleTexts = @(
    "Machine learning is a branch of artificial intelligence that focuses on building systems that learn from data.",
    "Artificial intelligence encompasses machine learning as a key component for creating intelligent systems.",
    "The field of computer science has evolved significantly since its inception in the mid-20th century.",
    "Python programming is widely used in data analysis due to its simplicity and extensive libraries.",
    "Data science combines statistics, programming, and domain knowledge to extract insights from data."
)

# Создаем папку если не существует
if (!(Test-Path "sample-data")) {
    New-Item -ItemType Directory -Path "sample-data"
}

# Создаем тестовые файлы
for ($i = 0; $i -lt $sampleTexts.Length; $i++) {
    $fileName = "sample-data/document$($i+1).txt"
    $sampleTexts[$i] | Out-File -FilePath $fileName -Encoding UTF8
    Write-Host "Created: $fileName"
}

Write-Host "Sample data generation complete!"
```

## 📝 Лицензия
MIT