import Foundation
import Combine
import PolySpatialRealityKit
import SwiftUI
import RealityKit
import UnityFramework

final class AppState: ObservableObject {

    @Published var selectedModel: String? = nil
    
    @Published var selectedSide: String? = nil

    static let shared = AppState()
    private init() {}

}