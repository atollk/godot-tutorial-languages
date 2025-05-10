// swift-tools-version: 6.1
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
    name: "tutorial",
    products: [
        // Products define the executables and libraries a package produces, making them visible to other packages.
        .library(
            name: "tutorial",
            type: .dynamic,
            targets: ["tutorial"],
        )
    ],
    dependencies: [
        .package(url: "https://github.com/migueldeicaza/SwiftGodot", revision: "0.50.0")
    ],
    targets: [
        // Targets are the basic building blocks of a package, defining a module or a test suite.
        // Targets can depend on other targets in this package and products from dependencies.
        .target(
            name: "tutorial", dependencies: ["SwiftGodot"]),
        .testTarget(
            name: "tutorialTests",
            dependencies: ["tutorial"]
        ),
    ]
)
