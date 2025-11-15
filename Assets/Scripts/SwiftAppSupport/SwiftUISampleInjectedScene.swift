// Any swift file whose name ends in "InjectedScene" is expected to contain
// a computed static "scene" property like the one below. It will be injected to the top
// level App's Scene. The name of the class/struct must match the name of the file.

import Foundation
import SwiftUI

struct SwiftUISampleInjectedScene {
    @SceneBuilder
    static var scene: some Scene {
        WindowGroup(id: "HomeView") {
            HomeView()
        }.defaultSize(width: 1600.0, height: 900.0)

        WindowGroup(id: "Configuration") {
            GUIConfigurationView()
        }

        WindowGroup(id: "ControlView") {
            ControlImmersiveView()
        }
    }
}

// @Observable types can be used to store and update data that is presented in SwiftUI views
// @Observable class ObjectCounter {
//     var cubeCount: Int = 0
//     var sphereCount: Int = 0
// }
