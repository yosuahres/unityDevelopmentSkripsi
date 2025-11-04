// Any swift file whose name ends in "InjectedScene" is expected to contain
// a computed static "scene" property like the one below. It will be injected to the top
// level App's Scene. The name of the class/struct must match the name of the file.

import Foundation
import SwiftUI

struct SwiftUISampleInjectedScene {
    @SceneBuilder
    static var scene: some Scene {
        WindowGroup(id: "HomeView") {
            // The sample defines a custom view, but you can also put your entire window's
            // structure here as you can with SwiftUI.
            HomeView()
        }.defaultSize(width: 1600.0, height: 900.0)

        WindowGroup(id: "ControlView") {
            // The sample defines a custom view, but you can also put your entire window's
            // structure here as you can with SwiftUI.
            ControlImmersiveView()
        }.defaultSize(width: 400.0, height: 600.0)

        // You can create multiple WindowGroups here for different wnidows;
        // they need a distinct id. If you include multiple items,
        // the scene property must be decorated with "@SceneBuilder" as above.
        // WindowGroup(id: "SimpleText") {
        //     Text("Hello World")
        // }
    }
}

// @Observable types can be used to store and update data that is presented in SwiftUI views
// @Observable class ObjectCounter {
//     var cubeCount: Int = 0
//     var sphereCount: Int = 0
// }
