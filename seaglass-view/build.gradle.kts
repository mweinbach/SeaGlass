plugins {
    alias(libs.plugins.android.library)
}

android {
    namespace = "dev.seaglass.view"
    compileSdk = 36

    defaultConfig {
        minSdk = 26
        consumerProguardFiles("consumer-rules.pro")
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
    api(project(":seaglass-core"))
    implementation(libs.androidx.core.ktx)

    testImplementation(libs.junit)
}
