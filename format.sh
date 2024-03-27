#!/usr/bin/env bash

rootDir=$(dirname "$0")

$rootDir/src/HotChocolate/AspNetCore/format.sh
$rootDir/src/HotChocolate/ApolloFederation/format.sh
$rootDir/src/HotChocolate/Data/format.sh
$rootDir/src/HotChocolate/MongoDb/format.sh
$rootDir/src/HotChocolate/ElasticSearch/format.sh
$rootDir/src/HotChocolate/Language/format.sh
$rootDir/src/HotChocolate/Stitching/format.sh
$rootDir/src/HotChocolate/Spatial/format.sh
