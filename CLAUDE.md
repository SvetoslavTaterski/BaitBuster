# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

BaitBuster е инструмент за откриване на фишинг имейли — дипломна работа (ТУ-София, магистър).
Бекендът е .NET 10, фронтендът е Angular 18. Езикът на проекта е български: UI текстът,
XML документацията и коментарите са на български, а идентификаторите и имената на тестовете —
на английски.

## Команди

```bash
# Бекенд
dotnet build BaitBuster.slnx
dotnet run --project src/BaitBuster.Api          # http://localhost:5289 (от launchSettings.json)

# Тестове
dotnet test tests/BaitBuster.Tests
dotnet test tests/BaitBuster.Tests --filter "FullyQualifiedName~HeaderMismatchRuleTests"
dotnet test tests/BaitBuster.Tests --filter "FullyQualifiedName~VerdictFollowsThirtyAndSixtyThresholds"

# Фронтенд (от frontend/)
npm start                                         # ng serve на :4200
npx ng build                                      # production билд
npx ng test --watch=false --browsers=ChromeHeadless

# EF Core миграции (dotnet-ef е локален инструмент — dotnet-tools.json)
dotnet ef migrations add <Име> --project src/BaitBuster.Api --startup-project src/BaitBuster.Api
```

Миграциите се прилагат автоматично при старт на API-то (`Database.Migrate()` в `Program.cs`) —
не се налага `database update` ръчно.

### Инструменти за ML частта

```bash
# 1. Сглобява корпуса от суровите CSV в data/raw/zenodo → един TSV (Label, Text, Source)
dotnet run --project tools/BaitBuster.DataPrep -- data/raw/zenodo data/corpus.tsv

# 2. Сравнява алгоритми, обучава победителя, записва model.zip + едноименен .json с метрики
dotnet run --project tools/BaitBuster.MlTraining -c Release -- data/corpus.tsv models/phishing-model.zip
#    --skip-cv пропуска кръстосаната проверка (бавната част), --folds N сменя броя дялове

# 3. Оценява цялото приложение (парсер + четирите правила) върху сурови .eml файлове
dotnet run --project tools/BaitBuster.CorpusEval -c Release -- <spamAssassinDir> models/phishing-model.zip
```

`MlTraining` мери само класификатора върху вече нормализиран текст; `CorpusEval` проверява
пълния път, включително че `EmlParser` се справя с истински MIME и че сборът от приноси
дава правилната присъда.

## Архитектура

### Детекцията е набор от независими правила

`IDetectionRule` (в `Detection/DetectionEngine.cs`) е единствената точка за разширяване.
Всяко правило описва само себе си — `RuleId`, `Name`, `Category`, `Description`, `MaxScore` —
затова `/api/rules` изброява регистрираните правила без да ги знае поименно, и ново правило
се появява в UI-то автоматично.

`DetectionEngine` само пуска всички правила и събира находките. Добавянето на правило значи
нов клас плюс един ред `AddSingleton<IDetectionRule, …>` в `Program.cs` — нищо друго.

**ML класификаторът е просто четвъртото правило** (`MlClassifierRule`), не отделен път през
системата. Това е съзнателно: сравнението „правила срещу ML" в дипломната работа изисква
двете да произвеждат съизмерими находки.

### Присъдата идва от константи, не от магически числа

`AnalysisReport.RiskScore` е сборът от приносите, ограничен до `MaxScore`. Праговете
`PhishingThreshold` (60) и `SuspiciousThreshold` (30) са публични константи и се излъчват
през `/api/rules`, за да не може UI-то да показва различно от това, което кодът прави.
При промяна на праг се пипа само `AnalysisReport`.

### Нормализацията на текста е споделен договор

`EmailTextNormalizer` се вика и при обучението (`DataPrep`/`MlTraining`), и при анализа
(`MlClassifierRule`). Ако поведението му се промени само от едната страна, моделът започва
да вижда думи, каквито не е срещал при обучението, и увереността му става безсмислена.
Затова правилата му са заковани с тестове.

### Фронтендът: състояние в store, изгледи в отделни компоненти

`AnalysisStore` (`providedIn: 'root'`) държи сесията за анализ — избран файл, поставен текст,
текущия доклад и откъде е дошъл (`ReportSource`). Живее извън компонентите, защото се дели:
„Нов анализ" и „Ръчен вход" показват един и същ доклад, „История" го зарежда, а входът трябва
да оцелее при смяна на таб.

`app.component` е само обвивка — меню, тема, превключване. Изгледите в `views/` се създават
при влизане и се унищожават при излизане (`@switch`), затова „Статистика" винаги показва
свежи числа. `ApiService` е единственото място, което знае адреси на endpoint-и.

## Среда: неочевидни неща, които чупят работата

**Бекендът не гради `.exe`.** `<UseAppHost>false</UseAppHost>` в `BaitBuster.Api.csproj` е
нарочно — корпоративни политики (AppLocker / Defender Application Control) блокират локално
компилиран неподписан `.exe` с „Access is denied". `dotnet.exe` е вече доверен, затова
приложението се изпълнява през него. Не връщай apphost-а.

**Базата живее извън хранилището.** SQLite файлът е в
`%LOCALAPPDATA%\BaitBuster\baitbuster.db`, а не до проекта — пътят на хранилището съдържа
кирилица (`ТУ-София\Дипломна работа Магистър`), а нативната SQLite библиотека на Windows
не се справя с non-ASCII сегменти: файлът мълчаливо не се създава там, където се очаква.

**Портът трябва да съвпада на две места.** `launchSettings.json` (5289) и
`frontend/src/environments/environment*.ts`. При разминаване фронтендът показва само
„Възникна грешка при анализа."

### Капани, които вече са ни стрували време

- **EF Core не превежда конструктор на собствен тип в `GroupBy` проекция.** Проектирай към
  анонимен тип, после мапвай след `ToListAsync()` — но дръж `OrderBy`/`Take` от страната на SQL.
- **SQLite не поддържа `ORDER BY` върху `DateTimeOffset`.** `AnalysisRecord.AnalyzedAt` е
  `DateTime` (UTC) и се увива обратно в `DateTimeOffset` чак в DTO-то.
- **`<` в условие на Angular `@if` се парсва като начало на HTML таг** и чупи целия блок.
  Изнеси сравнението в компонента (виж `ModelViewComponent.smallDataset`).
- **`as` алиас не работи върху `@else if`.** Вложи обикновен `@if` вътре.

## Конвенции

Тестовете следват структурата на `BaitBuster.Core` (`Detection/Rules/`, `Parsing/`, `Models/`).
`TestSupport/EmailBuilder` сглобява `ParsedEmail` — моделът има осем задължителни полета и без
builder намерението на теста се губи в шума. Правилата се тестват двупосочно: че хващат атака
и че **мълчат при нормален имейл** — лъжливите тревоги са по-скъпи от пропуските.

Иконите са ръчно написани inline SVG в `icon.component.ts`. Опитът с `@tabler/icons-webfont`
беше отхвърлен — влачеше `svgtofont` с 54 уязвимости заради няколко икони. Графиките в
„Статистика" са на чист CSS по същата причина.
