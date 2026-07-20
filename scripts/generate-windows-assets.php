<?php

declare(strict_types=1);

if (!extension_loaded('gd')) {
    fwrite(STDERR, "The PHP GD extension is required.\n");
    exit(1);
}

$outputDirectory = dirname(__DIR__) . '/packaging/windows/Assets';
if (!is_dir($outputDirectory) && !mkdir($outputDirectory, 0777, true) && !is_dir($outputDirectory)) {
    throw new RuntimeException("Unable to create {$outputDirectory}.");
}

$sourceSize = 1024;
$source = imagecreatetruecolor($sourceSize, $sourceSize);
if ($source === false) {
    throw new RuntimeException('Unable to create the source logo image.');
}

imageantialias($source, true);
$blue = imagecolorallocate($source, 29, 78, 216);
$white = imagecolorallocate($source, 255, 255, 255);
imagefill($source, 0, 0, $blue);

$drawThickEllipse = static function (
    GdImage $image,
    int $centerX,
    int $centerY,
    int $width,
    int $height,
    int $color,
    int $thickness,
): void {
    $radius = intdiv($thickness, 2);
    for ($offset = -$radius; $offset <= $radius; ++$offset) {
        imageellipse(
            $image,
            $centerX,
            $centerY,
            $width + ($offset * 2),
            $height + ($offset * 2),
            $color,
        );
    }
};

$left = (int) round($sourceSize * 0.22);
$right = (int) round($sourceSize * 0.78);
$topCenter = (int) round($sourceSize * 0.34);
$bottomCenter = (int) round($sourceSize * 0.66);
$ellipseHeight = (int) round($sourceSize * 0.24);
$stroke = (int) round($sourceSize * 0.063);
imagesetthickness($source, $stroke);
$center = intdiv($sourceSize, 2);
$drawThickEllipse($source, $center, $topCenter, $right - $left, $ellipseHeight, $white, $stroke);
imageline($source, $left, $topCenter, $left, $bottomCenter, $white);
imageline($source, $right, $topCenter, $right, $bottomCenter, $white);
$drawThickEllipse($source, $center, $bottomCenter, $right - $left, $ellipseHeight, $white, $stroke);

// GD lines have square caps; these circles keep the disk outline smooth after downsampling.
$capDiameter = $stroke;
foreach ([[$left, $topCenter], [$left, $bottomCenter], [$right, $topCenter], [$right, $bottomCenter]] as [$x, $y]) {
    imagefilledellipse($source, $x, $y, $capDiameter, $capDiameter, $white);
}

$targets = [
    dirname(__DIR__) . '/src/MultiDiskImager.App/Assets/AppIcon.png' => 1024,
    'StoreLogo.png' => 50,
    'Square44x44Logo.png' => 44,
    'Square150x150Logo.png' => 150,
];

foreach ($targets as $fileName => $size) {
    $target = imagecreatetruecolor($size, $size);
    if ($target === false) {
        throw new RuntimeException("Unable to create {$fileName}.");
    }
    imagecopyresampled($target, $source, 0, 0, 0, 0, $size, $size, $sourceSize, $sourceSize);
    $path = str_contains($fileName, '/') ? $fileName : $outputDirectory . '/' . $fileName;
    $targetDirectory = dirname($path);
    if (!is_dir($targetDirectory) && !mkdir($targetDirectory, 0777, true) && !is_dir($targetDirectory)) {
        throw new RuntimeException("Unable to create {$targetDirectory}.");
    }
    if (!imagepng($target, $path, 9)) {
        throw new RuntimeException("Unable to write {$path}.");
    }
    imagedestroy($target);
}

imagedestroy($source);
