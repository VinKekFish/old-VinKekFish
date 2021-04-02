# VinKekFish

Это проект небольшого криптографического приложения с ключом 4096 битов симметричного шифрования. Основной шифр - самодельный VinKekFish стойкостью 4096 битов (на основе комбинации keccak и Threefish).

Пока реализованы лишь только примитивы keccak версии 512 битов (максимальный из возможных) и Threefish версии 1024 битов (тоже максимальный; только на шифрование). Они находятся в отдельной библиотеке (cryptoprime/keccak.cs и cryptoprime/Threefish/Threefish_Static_Generated.cs), дающей возможность использовать её всем желающим программистам. Примеры использования смотрите в тестах в папке main_tests.

Описание VinKekFish (ещё не реализован) находится в файле "\main_tests\Задачи и другое\Криптография\Размышления\VinKekFish.md"
