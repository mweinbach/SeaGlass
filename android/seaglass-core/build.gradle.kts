plugins {
    alias(libs.plugins.android.library)
    `maven-publish`
}

group = "dev.seaglass"
version = "1.0.0"

android {
    namespace = "dev.seaglass.core"
    compileSdk = 36

    defaultConfig {
        minSdk = 26
        consumerProguardFiles("consumer-rules.pro")
    }

    publishing {
        singleVariant("release") {
            withSourcesJar()
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures {
        buildConfig = false
    }
}

kotlin {
    jvmToolchain(17)
}

dependencies {
    implementation(libs.androidx.core.ktx)
    api(libs.androidx.graphics.shapes)

    testImplementation(libs.junit)
}

afterEvaluate {
    publishing {
        publications {
            create<MavenPublication>("release") {
                from(components["release"])
                groupId = "dev.seaglass"
                artifactId = "seaglass-core"
                version = "1.0.0"
            }
        }
    }
}
