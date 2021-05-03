# VinKekFish

Это проект небольшого криптографического приложения с ключом 4096 битов симметричного шифрования. Основной шифр - самодельный VinKekFish стойкостью 4096 битов (на основе комбинации keccak и Threefish).


Реализованы примитивы keccak и Threefish:

keccak версии 512 битов (максимальный из возможных)

	cryptoprime/keccak.cs

	функция getHash512 - это пример, как рассчитать хеш
	https://github.com/VinKekFish/VinKekFish/blob/master/vinkekfish/keccak/keccak-20200918/keccak-base-20200918.cs

	генератор псевдослучайных чисел (очень медленно работает)
	https://github.com/VinKekFish/VinKekFish/blob/master/vinkekfish/keccak/keccak-20200918/Keccak_PRNG_20201128.cs


Threefish версии 1024 битов (тоже максимальный; только на шифрование)

	cryptoprime/Threefish/Threefish_Static_Generated.cs

  
Примеры использования смотрите в тестах в проекте main_tests.


VinKekFish пока не реализован

	Сам примитив
  
		cryptoprime.VinKekFish.VinKekFishBase_etalonK1

		VinKekFish/VinKekFish/VinKekFish-20210419/VinKekFish-20210419/VinKekFish_k1_base_20210419_keyGeneration.cs


Описание VinKekFish (ещё не реализован) находится в файле "\main_tests\Задачи и другое\Криптография\Размышления\VinKekFish.md"
