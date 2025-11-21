import Foundation
import Combine
import PolySpatialRealityKit
import SwiftUI
import RealityKit
import UnityFramework

final class AppState: ObservableObject {

    @Published var selectedModel: String? = nil
    
    @Published var selectedSide: String? = nil
    @Published var isRulerVisible: Bool = true

    static let shared = AppState()
    private init() {}

}
